using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository لمرتجعات المبيعات — TASK-017</summary>
internal class SalesReturnRepository
{
    // ── رقم المرتجع التالي ──────────────────────────────────
    public string NextReturnNumber()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(return_number FROM 5) AS INTEGER)), 0) + 1
            FROM public.sales_returns
            WHERE return_number LIKE 'RET-%'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar());
        return $"RET-{next:D5}";
    }

    // ── جلب فاتورة مبيعات بالرقم (للمرتجع) ────────────────
    public SalesInvoice? GetInvoiceByNumber(string invoiceNumber)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT si.id, si.invoice_number, si.invoice_date,
                   COALESCE(c.name, 'نقدي') AS customer_name,
                   si.net_total, si.payment_method, si.warehouse_id
            FROM public.sales_invoices si
            LEFT JOIN public.customers c ON c.id = si.customer_id
            WHERE si.invoice_number = @num
              AND si.status NOT IN ('cancelled')
            LIMIT 1
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("num", invoiceNumber.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var inv = new SalesInvoice
        {
            Id            = r.GetInt32(0),
            InvoiceNumber = r.GetString(1),
            InvoiceDate   = r.GetDateTime(2),
            CustomerName  = r.GetString(3),
            NetTotal      = r.GetDecimal(4),
            PaymentMethod = r.GetString(5),
            WarehouseId   = r.GetInt32(6)
        };
        r.Close();

        // جلب السطور مع الكميات المتبقية (المباعة - المرتجعة سابقاً)
        const string linesSql = """
            SELECT sil.item_id,
                   COALESCE(i.item_code, ''),
                   i.name_ar,
                   COALESCE(u.name_ar, u.name, '') AS unit_name,
                   sil.quantity AS sold_qty,
                   COALESCE(
                       (SELECT SUM(srl.quantity)
                        FROM public.sales_return_lines srl
                        JOIN public.sales_returns sr ON sr.id = srl.return_id
                        WHERE srl.item_id = sil.item_id
                          AND sr.original_invoice_id = @invId
                          AND sr.status = 'completed'), 0
                   ) AS already_returned,
                   sil.unit_price
            FROM public.sales_invoice_lines sil
            JOIN public.items i ON i.id = sil.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            WHERE sil.invoice_id = @invId
            """;
        using var cmd2 = new NpgsqlCommand(linesSql, conn);
        cmd2.Parameters.AddWithValue("invId", inv.Id);
        using var r2 = cmd2.ExecuteReader();
        while (r2.Read())
        {
            decimal sold      = r2.GetDecimal(4);
            decimal returned  = r2.GetDecimal(5);
            decimal available = sold - returned;
            if (available <= 0) continue; // تم إرجاع الكمية كلها مسبقاً

            inv.Lines.Add(new SalesInvoiceLine
            {
                ItemId    = r2.GetInt32(0),
                ItemCode  = r2.GetString(1),
                ItemName  = r2.GetString(2),
                UnitName  = r2.GetString(3),
                Quantity  = available, // الكمية المتاحة للإرجاع
                UnitPrice = r2.GetDecimal(6)
            });
        }
        return inv;
    }

    // ── حفظ المرتجع وإعادة المخزون ─────────────────────────
    public int SaveReturn(SalesReturn ret)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            const string ins = """
                INSERT INTO public.sales_returns
                    (return_number, original_invoice_id, cashier_id, warehouse_id,
                     return_date, total_amount, refund_method, status, notes)
                VALUES
                    (@num, @invId, @cashier, @wh,
                     @dt, @total, @refund, @status, @notes)
                RETURNING id
                """;
            using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("num",     ret.ReturnNumber);
            cmd.Parameters.AddWithValue("invId",   (object?)ret.OriginalInvoiceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cashier", ret.CashierId);
            cmd.Parameters.AddWithValue("wh",      ret.WarehouseId);
            cmd.Parameters.AddWithValue("dt",      ret.ReturnDate);
            cmd.Parameters.AddWithValue("total",   ret.TotalAmount);
            cmd.Parameters.AddWithValue("refund",  ret.RefundMethod);
            cmd.Parameters.AddWithValue("status",  ret.Status);
            cmd.Parameters.AddWithValue("notes",   (object?)ret.Notes ?? DBNull.Value);
            int retId = (int)(cmd.ExecuteScalar() ?? 0);

            foreach (var l in ret.Lines)
            {
                // سطر المرتجع
                const string insLine = """
                    INSERT INTO public.sales_return_lines
                        (return_id, item_id, quantity, unit_price, line_total)
                    VALUES (@retId, @item, @qty, @price, @total)
                    """;
                using var lCmd = new NpgsqlCommand(insLine, conn, tx);
                lCmd.Parameters.AddWithValue("retId", retId);
                lCmd.Parameters.AddWithValue("item",  l.ItemId);
                lCmd.Parameters.AddWithValue("qty",   l.ReturnQty);
                lCmd.Parameters.AddWithValue("price", l.UnitPrice);
                lCmd.Parameters.AddWithValue("total", l.LineTotal);
                lCmd.ExecuteNonQuery();

                // إعادة المخزون
                const string restore = """
                    UPDATE public.stock_levels
                    SET quantity = quantity + @qty, last_updated = NOW()
                    WHERE item_id = @item AND warehouse_id = @wh
                    """;
                using var sCmd = new NpgsqlCommand(restore, conn, tx);
                sCmd.Parameters.AddWithValue("qty",  l.ReturnQty);
                sCmd.Parameters.AddWithValue("item", l.ItemId);
                sCmd.Parameters.AddWithValue("wh",   ret.WarehouseId);
                sCmd.ExecuteNonQuery();
            }

            // تحديث حالة الفاتورة الأصلية إذا تم إرجاع الكل
            if (ret.OriginalInvoiceId.HasValue)
            {
                const string checkFull = """
                    UPDATE public.sales_invoices
                    SET status = 'returned'
                    WHERE id = @invId
                      AND (SELECT COALESCE(SUM(sil.quantity),0)
                           FROM public.sales_invoice_lines sil
                           WHERE sil.invoice_id = @invId)
                        <= (SELECT COALESCE(SUM(srl.quantity),0)
                            FROM public.sales_return_lines srl
                            JOIN public.sales_returns sr ON sr.id = srl.return_id
                            WHERE sr.original_invoice_id = @invId
                              AND sr.status = 'completed')
                    """;
                using var uCmd = new NpgsqlCommand(checkFull, conn, tx);
                uCmd.Parameters.AddWithValue("invId", ret.OriginalInvoiceId.Value);
                uCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return retId;
        }
        catch { tx.Rollback(); throw; }
    }

    // ── قائمة المرتجعات ──────────────────────────────────────
    public List<SalesReturn> GetAll(DateTime? from = null, DateTime? to = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT sr.id, sr.return_number, sr.return_date,
                   COALESCE(si.invoice_number, '—') AS inv_num,
                   COALESCE(c.name, 'نقدي')         AS cust_name,
                   COALESCE(u.display_name, '')      AS cashier,
                   sr.total_amount, sr.refund_method, sr.status,
                   COALESCE(sr.notes, '')
            FROM public.sales_returns sr
            LEFT JOIN public.sales_invoices si ON si.id = sr.original_invoice_id
            LEFT JOIN public.customers      c  ON c.id  = si.customer_id
            LEFT JOIN auth.users            u  ON u.id  = sr.cashier_id
            WHERE 1=1
            """;
        if (from.HasValue) sql += " AND sr.return_date >= @from";
        if (to.HasValue)   sql += " AND sr.return_date <  @to";
        sql += " ORDER BY sr.return_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (from.HasValue) cmd.Parameters.AddWithValue("from", from.Value.Date);
        if (to.HasValue)   cmd.Parameters.AddWithValue("to",   to.Value.Date.AddDays(1));

        using var r = cmd.ExecuteReader();
        var list = new List<SalesReturn>();
        while (r.Read())
            list.Add(new SalesReturn
            {
                Id                 = r.GetInt32(0),
                ReturnNumber       = r.GetString(1),
                ReturnDate         = r.GetDateTime(2),
                OriginalInvoiceNum = r.GetString(3),
                CustomerName       = r.GetString(4),
                CashierName        = r.GetString(5),
                TotalAmount        = r.GetDecimal(6),
                RefundMethod       = r.GetString(7),
                Status             = r.GetString(8),
                Notes              = r.GetString(9)
            });
        return list;
    }
}
