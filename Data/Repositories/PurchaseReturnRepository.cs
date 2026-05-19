using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository لمرتجعات المشتريات — TASK-013</summary>
internal class PurchaseReturnRepository
{
    // ── رقم المرتجع التالي ───────────────────────────────────
    public string NextReturnNumber()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(return_number FROM 5) AS INTEGER)), 0) + 1
            FROM public.purchase_returns
            WHERE return_number LIKE 'RET-%'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar());
        return $"RET-{next:D5}";
    }

    // ── قائمة المرتجعات ──────────────────────────────────────
    public List<PurchaseReturn> GetAll(DateTime? from = null, DateTime? to = null,
                                       string? status = null, int? supplierId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT r.id, r.return_number,
                   r.original_invoice_id, COALESCE(pi.invoice_number,'—'),
                   r.supplier_id, s.name,
                   r.warehouse_id, w.name,
                   r.return_date, r.total_amount, r.status,
                   COALESCE(r.notes,''), r.created_by, r.created_at
            FROM public.purchase_returns r
            JOIN public.suppliers  s  ON s.id = r.supplier_id
            JOIN public.warehouses w  ON w.id = r.warehouse_id
            LEFT JOIN public.purchase_invoices pi ON pi.id = r.original_invoice_id
            WHERE (@from       IS NULL OR r.return_date  >= @from)
              AND (@to         IS NULL OR r.return_date  <= @to)
              AND (@status     IS NULL OR r.status        = @status)
              AND (@supplierId IS NULL OR r.supplier_id   = @supplierId)
            ORDER BY r.return_date DESC, r.id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("from",       (object?)from       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("to",         (object?)to         ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status",     (object?)status     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("supplierId", (object?)supplierId ?? DBNull.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<PurchaseReturn>();
        while (r.Read()) list.Add(MapReturn(r));
        return list;
    }

    // ── مرتجع واحد مع سطوره ─────────────────────────────────
    public PurchaseReturn? GetById(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT r.id, r.return_number,
                   r.original_invoice_id, COALESCE(pi.invoice_number,'—'),
                   r.supplier_id, s.name,
                   r.warehouse_id, w.name,
                   r.return_date, r.total_amount, r.status,
                   COALESCE(r.notes,''), r.created_by, r.created_at
            FROM public.purchase_returns r
            JOIN public.suppliers  s  ON s.id = r.supplier_id
            JOIN public.warehouses w  ON w.id = r.warehouse_id
            LEFT JOIN public.purchase_invoices pi ON pi.id = r.original_invoice_id
            WHERE r.id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        var ret = MapReturn(reader);
        reader.Close();
        ret.Lines = GetLines(id, conn);
        return ret;
    }

    // ── سطور مرتجع ───────────────────────────────────────────
    private static List<PurchaseReturnLine> GetLines(int returnId, NpgsqlConnection conn)
    {
        const string sql = """
            SELECT rl.id, rl.return_id, rl.item_id,
                   i.item_code, i.name_ar,
                   COALESCE(u.name_ar, u.name, ''),
                   rl.quantity, rl.unit_price, rl.line_total
            FROM public.purchase_return_lines rl
            JOIN public.items i ON i.id = rl.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            WHERE rl.return_id = @ret
            ORDER BY rl.id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("ret", returnId);
        using var r = cmd.ExecuteReader();
        var lines = new List<PurchaseReturnLine>();
        while (r.Read())
            lines.Add(new PurchaseReturnLine
            {
                Id        = r.GetInt32(0),
                ReturnId  = r.GetInt32(1),
                ItemId    = r.GetInt32(2),
                ItemCode  = r.GetString(3),
                ItemName  = r.GetString(4),
                UnitName  = r.GetString(5),
                Quantity  = r.GetDecimal(6),
                UnitPrice = r.GetDecimal(7)
            });
        return lines;
    }

    // ── حفظ مسودة مرتجع ─────────────────────────────────────
    public int SaveDraft(PurchaseReturn ret)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            int id;
            if (ret.Id == 0)
            {
                const string ins = """
                    INSERT INTO public.purchase_returns
                        (return_number, original_invoice_id, supplier_id, warehouse_id,
                         return_date, total_amount, status, notes, created_by)
                    VALUES (@num, @invId, @sup, @wh, @dt, @total, 'draft', @notes, @by)
                    RETURNING id
                    """;
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                AddReturnParams(cmd, ret);
                id = (int)(cmd.ExecuteScalar() ?? 0);
            }
            else
            {
                const string upd = """
                    UPDATE public.purchase_returns SET
                        supplier_id=@sup, warehouse_id=@wh, return_date=@dt,
                        total_amount=@total, notes=@notes
                    WHERE id=@id AND status='draft'
                    """;
                using var cmd = new NpgsqlCommand(upd, conn, tx);
                AddReturnParams(cmd, ret);
                cmd.Parameters.AddWithValue("id", ret.Id);
                cmd.ExecuteNonQuery();
                id = ret.Id;

                using var delCmd = new NpgsqlCommand(
                    "DELETE FROM public.purchase_return_lines WHERE return_id=@id", conn, tx);
                delCmd.Parameters.AddWithValue("id", id);
                delCmd.ExecuteNonQuery();
            }

            InsertLines(id, ret.Lines, conn, tx);
            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    // ── اعتماد المرتجع (يخصم من المخزون) ───────────────────
    public void Approve(int returnId, int approvedBy)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // تحديث الحالة
            using var updCmd = new NpgsqlCommand("""
                UPDATE public.purchase_returns
                SET status = 'approved'
                WHERE id = @id AND status = 'draft'
                """, conn, tx);
            updCmd.Parameters.AddWithValue("id", returnId);
            updCmd.ExecuteNonQuery();

            // جلب warehouse_id
            int warehouseId;
            using (var wCmd = new NpgsqlCommand(
                "SELECT warehouse_id FROM public.purchase_returns WHERE id=@id", conn, tx))
            {
                wCmd.Parameters.AddWithValue("id", returnId);
                warehouseId = (int)(wCmd.ExecuteScalar() ?? 0);
            }

            // خصم المخزون لكل سطر
            var lines = GetLines(returnId, conn);
            foreach (var line in lines)
            {
                const string deductSql = """
                    UPDATE public.stock_levels
                    SET quantity = GREATEST(0, quantity - @qty),
                        last_updated = NOW()
                    WHERE item_id = @item AND warehouse_id = @wh
                    """;
                using var stockCmd = new NpgsqlCommand(deductSql, conn, tx);
                stockCmd.Parameters.AddWithValue("qty",  line.Quantity);
                stockCmd.Parameters.AddWithValue("item", line.ItemId);
                stockCmd.Parameters.AddWithValue("wh",   warehouseId);
                stockCmd.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ── جلب سطور فاتورة أصلية للمرتجع ──────────────────────
    /// <summary>يجلب سطور الفاتورة الأصلية ويحسب الكمية القابلة للإرجاع</summary>
    public List<PurchaseReturnLine> GetReturnableLines(int originalInvoiceId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT l.item_id, i.item_code, i.name_ar,
                   COALESCE(u.name_ar, u.name, ''),
                   l.quantity AS orig_qty,
                   COALESCE(returned.qty, 0) AS already_returned,
                   l.unit_price
            FROM public.purchase_invoice_lines l
            JOIN public.items i ON i.id = l.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            LEFT JOIN (
                SELECT rl.item_id, SUM(rl.quantity) AS qty
                FROM public.purchase_return_lines rl
                JOIN public.purchase_returns r ON r.id = rl.return_id
                WHERE r.original_invoice_id = @invId AND r.status = 'approved'
                GROUP BY rl.item_id
            ) returned ON returned.item_id = l.item_id
            WHERE l.invoice_id = @invId
              AND (l.quantity - COALESCE(returned.qty, 0)) > 0
            ORDER BY i.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("invId", originalInvoiceId);
        using var r = cmd.ExecuteReader();
        var lines = new List<PurchaseReturnLine>();
        while (r.Read())
        {
            decimal origQty     = r.GetDecimal(4);
            decimal alreadyRet  = r.GetDecimal(5);
            decimal maxQty      = origQty - alreadyRet;
            lines.Add(new PurchaseReturnLine
            {
                ItemId    = r.GetInt32(0),
                ItemCode  = r.GetString(1),
                ItemName  = r.GetString(2),
                UnitName  = r.GetString(3),
                MaxQty    = maxQty,
                Quantity  = maxQty,   // افتراضي: إرجاع الكل
                UnitPrice = r.GetDecimal(6)
            });
        }
        return lines;
    }

    // ── helpers ──────────────────────────────────────────────
    private static void AddReturnParams(NpgsqlCommand cmd, PurchaseReturn ret)
    {
        cmd.Parameters.AddWithValue("num",   ret.ReturnNumber);
        cmd.Parameters.AddWithValue("invId", (object?)ret.OriginalInvoiceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sup",   ret.SupplierId);
        cmd.Parameters.AddWithValue("wh",    ret.WarehouseId);
        cmd.Parameters.AddWithValue("dt",    ret.ReturnDate);
        cmd.Parameters.AddWithValue("total", ret.TotalAmount);
        cmd.Parameters.AddWithValue("notes", (object?)ret.Notes  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("by",    (object?)ret.CreatedBy ?? DBNull.Value);
    }

    private static void InsertLines(int returnId, List<PurchaseReturnLine> lines,
                                    NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        const string sql = """
            INSERT INTO public.purchase_return_lines
                (return_id, item_id, quantity, unit_price, line_total)
            VALUES (@ret, @item, @qty, @price, @total)
            """;
        foreach (var l in lines)
        {
            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("ret",   returnId);
            cmd.Parameters.AddWithValue("item",  l.ItemId);
            cmd.Parameters.AddWithValue("qty",   l.Quantity);
            cmd.Parameters.AddWithValue("price", l.UnitPrice);
            cmd.Parameters.AddWithValue("total", l.Quantity * l.UnitPrice);
            cmd.ExecuteNonQuery();
        }
    }

    private static PurchaseReturn MapReturn(NpgsqlDataReader r) => new()
    {
        Id                 = r.GetInt32(0),
        ReturnNumber       = r.GetString(1),
        OriginalInvoiceId  = r.IsDBNull(2) ? null : r.GetInt32(2),
        OriginalInvoiceNum = r.GetString(3),
        SupplierId         = r.GetInt32(4),
        SupplierName       = r.GetString(5),
        WarehouseId        = r.GetInt32(6),
        WarehouseName      = r.GetString(7),
        ReturnDate         = r.GetDateTime(8),
        TotalAmount        = r.GetDecimal(9),
        Status             = r.GetString(10),
        Notes              = r.GetString(11),
        CreatedBy          = r.IsDBNull(12) ? null : r.GetInt32(12),
        CreatedAt          = r.GetDateTime(13)
    };
}
