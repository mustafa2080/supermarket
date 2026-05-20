using Npgsql;
using NpgsqlTypes;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository لفواتير الشراء — TASK-012</summary>
internal class PurchaseRepository
{
    // ── توليد رقم الفاتورة التالي ───────────────────────────
    public string NextInvoiceNumber()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(invoice_number FROM 5) AS INTEGER)), 0) + 1
            FROM public.purchase_invoices
            WHERE invoice_number LIKE 'PUR-%'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar());
        return $"PUR-{next:D5}";
    }

    // ── قائمة الفواتير ───────────────────────────────────────
    public List<PurchaseInvoice> GetAll(DateTime? from = null, DateTime? to = null,
                                        string? status = null, int? supplierId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT pi.id, pi.invoice_number, pi.supplier_id, s.name,
                   pi.warehouse_id, w.name, pi.invoice_date, pi.payment_method,
                   pi.subtotal, pi.discount, pi.tax_amount, pi.net_total,
                   pi.paid_amount, pi.remaining, pi.status, COALESCE(pi.notes,''),
                   pi.created_by, pi.created_at
            FROM public.purchase_invoices pi
            JOIN public.suppliers s  ON s.id = pi.supplier_id
            JOIN public.warehouses w ON w.id = pi.warehouse_id
            WHERE (@from       IS NULL OR pi.invoice_date >= @from)
              AND (@to         IS NULL OR pi.invoice_date <= @to)
              AND (@status     IS NULL OR pi.status = @status)
              AND (@supplierId IS NULL OR pi.supplier_id = @supplierId)
            ORDER BY pi.invoice_date DESC, pi.id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("from",       NpgsqlTypes.NpgsqlDbType.Date)    { Value = (object?)from       ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("to",         NpgsqlTypes.NpgsqlDbType.Date)    { Value = (object?)to         ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("status",     NpgsqlTypes.NpgsqlDbType.Text)    { Value = (object?)status     ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("supplierId", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)supplierId ?? DBNull.Value });
        using var r = cmd.ExecuteReader();
        var list = new List<PurchaseInvoice>();
        while (r.Read()) list.Add(MapInvoice(r));
        return list;
    }

    // ── تفاصيل فاتورة واحدة مع سطورها ──────────────────────
    public PurchaseInvoice? GetById(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT pi.id, pi.invoice_number, pi.supplier_id, s.name,
                   pi.warehouse_id, w.name, pi.invoice_date, pi.payment_method,
                   pi.subtotal, pi.discount, pi.tax_amount, pi.net_total,
                   pi.paid_amount, pi.remaining, pi.status, COALESCE(pi.notes,''),
                   pi.created_by, pi.created_at
            FROM public.purchase_invoices pi
            JOIN public.suppliers s  ON s.id = pi.supplier_id
            JOIN public.warehouses w ON w.id = pi.warehouse_id
            WHERE pi.id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        var inv = MapInvoice(r);
        r.Close();
        inv.Lines = GetLines(id, conn);
        return inv;
    }

    // ── سطور الفاتورة ────────────────────────────────────────
    private static List<PurchaseInvoiceLine> GetLines(int invoiceId, NpgsqlConnection conn)
    {
        const string sql = """
            SELECT l.id, l.invoice_id, l.item_id,
                   i.item_code, i.name_ar,
                   COALESCE(u.name_ar, u.name, ''),
                   l.quantity, l.unit_price, l.discount, l.tax_rate, l.tax_amount, l.line_total,
                   COALESCE(l.notes,'')
            FROM public.purchase_invoice_lines l
            JOIN public.items i ON i.id = l.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            WHERE l.invoice_id = @inv
            ORDER BY l.id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("inv", invoiceId);
        using var r = cmd.ExecuteReader();
        var lines = new List<PurchaseInvoiceLine>();
        while (r.Read())
            lines.Add(new PurchaseInvoiceLine
            {
                Id        = r.GetInt32(0),
                InvoiceId = r.GetInt32(1),
                ItemId    = r.GetInt32(2),
                ItemCode  = r.GetString(3),
                ItemName  = r.GetString(4),
                UnitName  = r.GetString(5),
                Quantity  = r.GetDecimal(6),
                UnitPrice = r.GetDecimal(7),
                Discount  = r.GetDecimal(8),
                TaxRate   = r.GetDecimal(9),
                TaxAmount = r.GetDecimal(10),
                LineTotal = r.GetDecimal(11),
                Notes     = r.GetString(12)
            });
        return lines;
    }

    // ── حفظ مسودة أو تعديل مسودة ────────────────────────────
    public int SaveDraft(PurchaseInvoice inv)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            int id;
            if (inv.Id == 0)
            {
                const string ins = """
                    INSERT INTO public.purchase_invoices
                        (invoice_number, supplier_id, warehouse_id, invoice_date,
                         payment_method, subtotal, discount, tax_amount, net_total,
                         paid_amount, remaining, status, notes, created_by)
                    VALUES (@num,@sup,@wh,@dt,@pay,@sub,@disc,@tax,@net,@paid,@rem,'draft',@notes,@by)
                    RETURNING id
                    """;
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                AddInvoiceParams(cmd, inv);
                id = (int)(cmd.ExecuteScalar() ?? 0);
            }
            else
            {
                const string upd = """
                    UPDATE public.purchase_invoices SET
                        supplier_id=@sup, warehouse_id=@wh, invoice_date=@dt,
                        payment_method=@pay, subtotal=@sub, discount=@disc,
                        tax_amount=@tax, net_total=@net, paid_amount=@paid,
                        remaining=@rem, notes=@notes
                    WHERE id=@id AND status='draft'
                    """;
                using var cmd = new NpgsqlCommand(upd, conn, tx);
                AddInvoiceParams(cmd, inv);
                cmd.Parameters.AddWithValue("id", inv.Id);
                cmd.ExecuteNonQuery();
                id = inv.Id;

                // حذف السطور القديمة وإعادة الإدراج
                using var delCmd = new NpgsqlCommand(
                    "DELETE FROM public.purchase_invoice_lines WHERE invoice_id = @id", conn, tx);
                delCmd.Parameters.AddWithValue("id", id);
                delCmd.ExecuteNonQuery();
            }

            InsertLines(id, inv.Lines, conn, tx);
            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    // ── اعتماد الفاتورة (يُحدِّث المخزون) ──────────────────
    public void Approve(int invoiceId, int approvedBy)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // تحديث حالة الفاتورة
            const string updInv = """
                UPDATE public.purchase_invoices SET
                    status = 'approved',
                    approved_by = @by,
                    approved_at = NOW()
                WHERE id = @id AND status = 'draft'
                """;
            using var cmd1 = new NpgsqlCommand(updInv, conn, tx);
            cmd1.Parameters.AddWithValue("by", approvedBy);
            cmd1.Parameters.AddWithValue("id", invoiceId);
            cmd1.ExecuteNonQuery();

            // جلب السطور لتحديث المخزون
            var lines = GetLines(invoiceId, conn);

            // جلب warehouse_id
            int warehouseId;
            using (var wCmd = new NpgsqlCommand(
                "SELECT warehouse_id FROM public.purchase_invoices WHERE id = @id", conn, tx))
            {
                wCmd.Parameters.AddWithValue("id", invoiceId);
                warehouseId = (int)(wCmd.ExecuteScalar() ?? 0);
            }

            // تحديث/إنشاء stock_levels لكل سطر
            foreach (var line in lines)
            {
                const string upsertStock = """
                    INSERT INTO public.stock_levels (item_id, warehouse_id, quantity, last_updated)
                    VALUES (@item, @wh, @qty, NOW())
                    ON CONFLICT (item_id, warehouse_id)
                    DO UPDATE SET
                        quantity = stock_levels.quantity + EXCLUDED.quantity,
                        last_updated = NOW()
                    """;
                using var stockCmd = new NpgsqlCommand(upsertStock, conn, tx);
                stockCmd.Parameters.AddWithValue("item", line.ItemId);
                stockCmd.Parameters.AddWithValue("wh",   warehouseId);
                stockCmd.Parameters.AddWithValue("qty",  line.Quantity);
                stockCmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ── إلغاء فاتورة مسودة ──────────────────────────────────
    public void Cancel(int invoiceId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd  = new NpgsqlCommand(
            "UPDATE public.purchase_invoices SET status='cancelled' WHERE id=@id AND status='draft'", conn);
        cmd.Parameters.AddWithValue("id", invoiceId);
        cmd.ExecuteNonQuery();
    }

    // ── helpers ──────────────────────────────────────────────
    private static void AddInvoiceParams(NpgsqlCommand cmd, PurchaseInvoice inv)
    {
        cmd.Parameters.AddWithValue("num",   inv.InvoiceNumber);
        cmd.Parameters.AddWithValue("sup",   inv.SupplierId);
        cmd.Parameters.AddWithValue("wh",    inv.WarehouseId);
        cmd.Parameters.AddWithValue("dt",    inv.InvoiceDate);
        cmd.Parameters.AddWithValue("pay",   inv.PaymentMethod);
        cmd.Parameters.AddWithValue("sub",   inv.Subtotal);
        cmd.Parameters.AddWithValue("disc",  inv.Discount);
        cmd.Parameters.AddWithValue("tax",   inv.TaxAmount);
        cmd.Parameters.AddWithValue("net",   inv.NetTotal);
        cmd.Parameters.AddWithValue("paid",  inv.PaidAmount);
        cmd.Parameters.AddWithValue("rem",   inv.Remaining);
        cmd.Parameters.AddWithValue("notes", (object?)inv.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("by",    (object?)inv.CreatedBy ?? DBNull.Value);
    }

    private static void InsertLines(int invoiceId, List<PurchaseInvoiceLine> lines,
                                    NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        const string sql = """
            INSERT INTO public.purchase_invoice_lines
                (invoice_id, item_id, quantity, unit_price, discount, tax_rate, tax_amount, line_total, notes)
            VALUES (@inv, @item, @qty, @price, @disc, @taxR, @taxA, @total, @notes)
            """;
        foreach (var l in lines)
        {
            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("inv",   invoiceId);
            cmd.Parameters.AddWithValue("item",  l.ItemId);
            cmd.Parameters.AddWithValue("qty",   l.Quantity);
            cmd.Parameters.AddWithValue("price", l.UnitPrice);
            cmd.Parameters.AddWithValue("disc",  l.Discount);
            cmd.Parameters.AddWithValue("taxR",  l.TaxRate);
            cmd.Parameters.AddWithValue("taxA",  l.TaxAmount);
            cmd.Parameters.AddWithValue("total", l.LineTotal);
            cmd.Parameters.AddWithValue("notes", (object?)l.Notes ?? DBNull.Value);
        }
    }

    private static PurchaseInvoice MapInvoice(NpgsqlDataReader r) => new()
    {
        Id            = r.GetInt32(0),
        InvoiceNumber = r.GetString(1),
        SupplierId    = r.GetInt32(2),
        SupplierName  = r.GetString(3),
        WarehouseId   = r.GetInt32(4),
        WarehouseName = r.GetString(5),
        InvoiceDate   = r.GetDateTime(6),
        PaymentMethod = r.GetString(7),
        Subtotal      = r.GetDecimal(8),
        Discount      = r.GetDecimal(9),
        TaxAmount     = r.GetDecimal(10),
        NetTotal      = r.GetDecimal(11),
        PaidAmount    = r.GetDecimal(12),
        Remaining     = r.GetDecimal(13),
        Status        = r.GetString(14),
        Notes         = r.GetString(15),
        CreatedBy     = r.IsDBNull(16) ? null : r.GetInt32(16),
        CreatedAt     = r.GetDateTime(17)
    };
}
