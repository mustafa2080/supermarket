using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository لفواتير المبيعات — TASK-014</summary>
internal class SalesRepository
{
    // ── رقم الفاتورة التالي ──────────────────────────────────
    public string NextInvoiceNumber()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(invoice_number FROM 5) AS INTEGER)), 0) + 1
            FROM public.sales_invoices
            WHERE invoice_number LIKE 'SAL-%'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar());
        return $"SAL-{next:D5}";
    }

    // ── بحث أصناف (POS) ─────────────────────────────────────
    /// <summary>بحث بالاسم أو الباركود أو الكود — يرجع فقط الأصناف النشطة</summary>
    public List<Item> SearchItems(string query, int warehouseId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT i.id, i.item_code, i.name_ar, i.barcode,
                   COALESCE(u.name_ar, u.name, ''),
                   i.retail_price, i.tax_rate,
                   COALESCE(sl.quantity, 0) AS stock_qty
            FROM public.items i
            LEFT JOIN public.units u ON u.id = i.unit_id
            LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
            WHERE i.is_active = true
              AND (
                  i.name_ar    ILIKE @q
               OR i.barcode    ILIKE @q
               OR i.item_code  ILIKE @q
              )
            ORDER BY i.name_ar
            LIMIT 30
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q",  $"%{query}%");
        cmd.Parameters.AddWithValue("wh", warehouseId);
        using var r = cmd.ExecuteReader();
        var list = new List<Item>();
        while (r.Read())
            list.Add(new Item
            {
                Id           = r.GetInt32(0),
                ItemCode     = r.GetString(1),
                NameAr       = r.GetString(2),
                Barcode      = r.GetString(3),
                UnitName     = r.GetString(4),
                RetailPrice  = r.GetDecimal(5),
                TaxRate      = r.GetDecimal(6),
                CurrentStock = r.GetDecimal(7)
            });
        return list;
    }

    /// <summary>جلب صنف بالباركود بالضبط (مسح سريع)</summary>
    public Item? GetByBarcode(string barcode, int warehouseId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT i.id, i.item_code, i.name_ar, i.barcode,
                   COALESCE(u.name_ar, u.name, ''),
                   i.retail_price, i.tax_rate,
                   COALESCE(sl.quantity, 0)
            FROM public.items i
            LEFT JOIN public.units u ON u.id = i.unit_id
            LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
            WHERE i.barcode = @bc AND i.is_active = true
            LIMIT 1
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("bc", barcode);
        cmd.Parameters.AddWithValue("wh", warehouseId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Item
        {
            Id           = r.GetInt32(0),
            ItemCode     = r.GetString(1),
            NameAr       = r.GetString(2),
            Barcode      = r.GetString(3),
            UnitName     = r.GetString(4),
            RetailPrice  = r.GetDecimal(5),
            TaxRate      = r.GetDecimal(6),
            CurrentStock = r.GetDecimal(7)
        };
    }

    // ── حفظ الفاتورة وتحديث المخزون ─────────────────────────
    public int SaveInvoice(SalesInvoice inv)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            const string ins = """
                INSERT INTO public.sales_invoices
                    (invoice_number, customer_id, warehouse_id, cashier_id, shift_id,
                     invoice_date, payment_method, subtotal, discount, tax_amount,
                     net_total, paid_amount, change_amount, loyalty_redeemed, status, notes)
                VALUES
                    (@num, @cust, @wh, @cashier, @shift,
                     @dt, @pay, @sub, @disc, @tax,
                     @net, @paid, @change, @loyalty, @status, @notes)
                RETURNING id
                """;
            using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("num",     inv.InvoiceNumber);
            cmd.Parameters.AddWithValue("cust",    (object?)inv.CustomerId    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("wh",      inv.WarehouseId);
            cmd.Parameters.AddWithValue("cashier", inv.CashierId);
            cmd.Parameters.AddWithValue("shift",   (object?)inv.ShiftId      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dt",      inv.InvoiceDate);
            cmd.Parameters.AddWithValue("pay",     inv.PaymentMethod);
            cmd.Parameters.AddWithValue("sub",     inv.Subtotal);
            cmd.Parameters.AddWithValue("disc",    inv.Discount);
            cmd.Parameters.AddWithValue("tax",     inv.TaxAmount);
            cmd.Parameters.AddWithValue("net",     inv.NetTotal);
            cmd.Parameters.AddWithValue("paid",    inv.PaidAmount);
            cmd.Parameters.AddWithValue("change",  inv.ChangeAmount);
            cmd.Parameters.AddWithValue("loyalty", inv.LoyaltyRedeemed);
            cmd.Parameters.AddWithValue("status",  inv.Status);
            cmd.Parameters.AddWithValue("notes",   (object?)inv.Notes        ?? DBNull.Value);
            int id = (int)(cmd.ExecuteScalar() ?? 0);

            // سطور الفاتورة
            foreach (var l in inv.Lines)
            {
                decimal taxAmt  = l.Quantity * l.UnitPrice * (l.TaxRate / 100m);
                decimal lineTot = l.Quantity * l.UnitPrice - l.Discount + taxAmt;

                const string insLine = """
                    INSERT INTO public.sales_invoice_lines
                        (invoice_id, item_id, quantity, unit_price, discount,
                         promotion_id, tax_rate, tax_amount, line_total)
                    VALUES (@inv, @item, @qty, @price, @disc,
                            @promo, @taxRate, @taxAmt, @total)
                    """;
                using var lCmd = new NpgsqlCommand(insLine, conn, tx);
                lCmd.Parameters.AddWithValue("inv",     id);
                lCmd.Parameters.AddWithValue("item",    l.ItemId);
                lCmd.Parameters.AddWithValue("qty",     l.Quantity);
                lCmd.Parameters.AddWithValue("price",   l.UnitPrice);
                lCmd.Parameters.AddWithValue("disc",    l.Discount);
                lCmd.Parameters.AddWithValue("promo",   (object?)l.PromotionId ?? DBNull.Value);
                lCmd.Parameters.AddWithValue("taxRate", l.TaxRate);
                lCmd.Parameters.AddWithValue("taxAmt",  taxAmt);
                lCmd.Parameters.AddWithValue("total",   lineTot);
                lCmd.ExecuteNonQuery();

                // خصم المخزون
                const string deduct = """
                    UPDATE public.stock_levels
                    SET quantity = GREATEST(0, quantity - @qty), last_updated = NOW()
                    WHERE item_id = @item AND warehouse_id = @wh
                    """;
                using var sCmd = new NpgsqlCommand(deduct, conn, tx);
                sCmd.Parameters.AddWithValue("qty",  l.Quantity);
                sCmd.Parameters.AddWithValue("item", l.ItemId);
                sCmd.Parameters.AddWithValue("wh",   inv.WarehouseId);
                sCmd.ExecuteNonQuery();
            }

            // نقاط الولاء (اكتساب)
            if (inv.CustomerId.HasValue && inv.NetTotal > 0)
            {
                decimal pts = Math.Floor(inv.NetTotal / 10m);
                const string addPts = """
                    UPDATE public.customers
                    SET loyalty_points = loyalty_points + @pts - @redeemed
                    WHERE id = @id
                    """;
                using var pCmd = new NpgsqlCommand(addPts, conn, tx);
                pCmd.Parameters.AddWithValue("pts",      pts);
                pCmd.Parameters.AddWithValue("redeemed", inv.LoyaltyRedeemed);
                pCmd.Parameters.AddWithValue("id",       inv.CustomerId.Value);
                pCmd.ExecuteNonQuery();
            }

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    // ── جلب العملاء ─────────────────────────────────────────
    public List<Customer> GetCustomers(bool activeOnly = true)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = "SELECT id, COALESCE(code,''), name, COALESCE(phone,''), credit_limit, loyalty_points FROM public.customers";
        if (activeOnly) sql += " WHERE is_active = true";
        sql += " ORDER BY name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Customer>();
        while (r.Read())
            list.Add(new Customer
            {
                Id            = r.GetInt32(0),
                Code          = r.GetString(1),
                Name          = r.GetString(2),
                Phone         = r.GetString(3),
                CreditLimit   = r.GetDecimal(4),
                LoyaltyPoints = r.GetDecimal(5)
            });
        return list;
    }

    // ── جلب المستودع الافتراضي ──────────────────────────────
    public Warehouse? GetDefaultWarehouse()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT id, name FROM public.warehouses
            WHERE is_active = true AND is_default = true
            LIMIT 1
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Warehouse { Id = r.GetInt32(0), Name = r.GetString(1) };
    }
}
