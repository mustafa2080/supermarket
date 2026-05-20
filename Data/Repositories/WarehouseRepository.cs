using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للمستودعات والمخزون — TASK-018</summary>
internal class WarehouseRepository
{
    // ══ المستودعات ════════════════════════════════════════════

    public List<Warehouse> GetAll()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = "SELECT id, name, COALESCE(location,''), is_default, is_active FROM public.warehouses ORDER BY is_default DESC, name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Warehouse>();
        while (r.Read())
            list.Add(new Warehouse { Id = r.GetInt32(0), Name = r.GetString(1), Location = r.GetString(2), IsDefault = r.GetBoolean(3), IsActive = r.GetBoolean(4) });
        return list;
    }

    public void Save(Warehouse w)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            if (w.IsDefault)
            {
                using var clr = new NpgsqlCommand("UPDATE public.warehouses SET is_default = false", conn, tx);
                clr.ExecuteNonQuery();
            }
            if (w.Id == 0)
            {
                const string ins = "INSERT INTO public.warehouses (name, location, is_default, is_active) VALUES (@n,@l,@d,@a)";
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                cmd.Parameters.AddWithValue("n", w.Name);
                cmd.Parameters.AddWithValue("l", (object?)w.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("d", w.IsDefault);
                cmd.Parameters.AddWithValue("a", w.IsActive);
                cmd.ExecuteNonQuery();
            }
            else
            {
                const string upd = "UPDATE public.warehouses SET name=@n, location=@l, is_default=@d, is_active=@a WHERE id=@id";
                using var cmd = new NpgsqlCommand(upd, conn, tx);
                cmd.Parameters.AddWithValue("n",  w.Name);
                cmd.Parameters.AddWithValue("l",  (object?)w.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("d",  w.IsDefault);
                cmd.Parameters.AddWithValue("a",  w.IsActive);
                cmd.Parameters.AddWithValue("id", w.Id);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ مستوى المخزون ════════════════════════════════════════

    public List<StockLevel> GetStockLevels(int warehouseId, string? search = null, string? groupFilter = null, string? statusFilter = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT i.id, COALESCE(i.item_code,''), i.name_ar,
                   COALESCE(g.name_ar, g.name, '') AS grp,
                   COALESCE(u.name_ar, u.name, '') AS unit,
                   @wh,
                   COALESCE(sl.quantity, 0),
                   COALESCE(i.reorder_point, 0),
                   COALESCE(i.purchase_price, 0)
            FROM public.items i
            LEFT JOIN public.item_groups g  ON g.id  = i.group_id
            LEFT JOIN public.units       u  ON u.id  = i.unit_id
            LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
            WHERE i.is_active = true
            """;
        if (!string.IsNullOrEmpty(search))      sql += " AND (i.name_ar ILIKE @q OR i.item_code ILIKE @q OR i.barcode ILIKE @q)";
        if (!string.IsNullOrEmpty(groupFilter)) sql += " AND g.name_ar = @grp";
        if (statusFilter == "منخفض")            sql += " AND COALESCE(sl.quantity,0) > 0 AND COALESCE(sl.quantity,0) <= i.reorder_point";
        if (statusFilter == "نفاد")             sql += " AND COALESCE(sl.quantity,0) <= 0";
        if (statusFilter == "كافي")             sql += " AND COALESCE(sl.quantity,0) > i.reorder_point";
        sql += " ORDER BY i.name_ar";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("wh", warehouseId);
        if (!string.IsNullOrEmpty(search))      cmd.Parameters.AddWithValue("q",   $"%{search}%");
        if (!string.IsNullOrEmpty(groupFilter)) cmd.Parameters.AddWithValue("grp", groupFilter);

        using var r = cmd.ExecuteReader();
        var list = new List<StockLevel>();
        while (r.Read())
            list.Add(new StockLevel
            {
                ItemId        = r.GetInt32(0),
                ItemCode      = r.GetString(1),
                ItemName      = r.GetString(2),
                GroupName     = r.GetString(3),
                UnitName      = r.GetString(4),
                WarehouseId   = r.GetInt32(5),
                Quantity      = r.GetDecimal(6),
                ReorderPoint  = r.GetDecimal(7),
                PurchasePrice = r.GetDecimal(8)
            });
        return list;
    }

    /// <summary>إحصاء الأصناف منخفضة/نافدة المخزون</summary>
    public (int Low, int OutOfStock) GetAlertCounts(int warehouseId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT
                COUNT(*) FILTER (WHERE sl.quantity > 0 AND sl.quantity <= i.reorder_point),
                COUNT(*) FILTER (WHERE COALESCE(sl.quantity,0) <= 0)
            FROM public.items i
            LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
            WHERE i.is_active = true
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("wh", warehouseId);
        using var r = cmd.ExecuteReader();
        r.Read();
        return ((int)r.GetInt64(0), (int)r.GetInt64(1));
    }

    // ══ الرصيد الافتتاحي ════════════════════════════════════

    /// <summary>حفظ/تحديث الرصيد الافتتاحي لمجموعة أصناف في مستودع</summary>
    public void SaveOpeningStock(int warehouseId, List<(int itemId, decimal qty)> lines)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            foreach (var (itemId, qty) in lines)
            {
                const string upsert = """
                    INSERT INTO public.stock_levels (item_id, warehouse_id, quantity, last_updated)
                    VALUES (@item, @wh, @qty, NOW())
                    ON CONFLICT (item_id, warehouse_id)
                    DO UPDATE SET quantity = @qty, last_updated = NOW()
                    """;
                using var cmd = new NpgsqlCommand(upsert, conn, tx);
                cmd.Parameters.AddWithValue("item", itemId);
                cmd.Parameters.AddWithValue("wh",   warehouseId);
                cmd.Parameters.AddWithValue("qty",  qty);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ قوائم مساعدة ═════════════════════════════════════════

    public List<string> GetGroupNames()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = "SELECT COALESCE(name_ar, name) FROM public.item_groups WHERE is_active = true ORDER BY 1";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<string>();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    // ══ جرد المخزون (TASK-019) ═══════════════════════════════

    /// <summary>إنشاء جلسة جرد جديدة وإرجاع الـ ID</summary>
    public int CreateInventoryCount(int warehouseId, string notes, int createdByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // رقم الجرد تلقائي
            var num = "JRD-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            const string sql = """
                INSERT INTO public.inventory_counts (count_number, warehouse_id, count_date, status, notes, created_by)
                VALUES (@num, @wh, @dt, 'in_progress', @notes, @by)
                RETURNING id
                """;
            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("num",   num);
            cmd.Parameters.AddWithValue("wh",    warehouseId);
            cmd.Parameters.AddWithValue("dt",    DateTime.Today);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("by",    createdByUserId);
            var id = (int)cmd.ExecuteScalar()!;

            // تحميل الأصناف الحالية من المستودع كسطور جرد
            const string items = """
                INSERT INTO public.inventory_count_lines (count_id, item_id, system_qty, counted_qty)
                SELECT @cid, i.id, COALESCE(sl.quantity,0), 0
                FROM public.items i
                LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
                WHERE i.is_active = true
                """;
            using var cmd2 = new NpgsqlCommand(items, conn, tx);
            cmd2.Parameters.AddWithValue("cid", id);
            cmd2.Parameters.AddWithValue("wh",  warehouseId);
            cmd2.ExecuteNonQuery();

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>قائمة جلسات الجرد</summary>
    public List<InventoryCount> GetInventoryCounts(int? warehouseId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT ic.id, ic.count_number, ic.warehouse_id, w.name,
                   ic.count_date, ic.status, COALESCE(ic.notes,''),
                   COALESCE(u1.full_name,''), COALESCE(u2.full_name,'')
            FROM public.inventory_counts ic
            JOIN public.warehouses w ON w.id = ic.warehouse_id
            LEFT JOIN auth.users u1 ON u1.id = ic.created_by
            LEFT JOIN auth.users u2 ON u2.id = ic.approved_by
            WHERE 1=1
            """;
        if (warehouseId.HasValue) sql += " AND ic.warehouse_id = @wh";
        sql += " ORDER BY ic.count_date DESC, ic.id DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (warehouseId.HasValue) cmd.Parameters.AddWithValue("wh", warehouseId.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<InventoryCount>();
        while (r.Read())
            list.Add(new InventoryCount
            {
                Id            = r.GetInt32(0),
                CountNumber   = r.GetString(1),
                WarehouseId   = r.GetInt32(2),
                WarehouseName = r.GetString(3),
                CountDate     = r.GetDateTime(4),
                Status        = r.GetString(5),
                Notes         = r.GetString(6),
                CreatedBy     = r.GetString(7),
                ApprovedBy    = r.GetString(8)
            });
        return list;
    }

    /// <summary>سطور جلسة جرد مع بيانات الصنف</summary>
    public List<InventoryCountLine> GetCountLines(int countId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT icl.item_id, COALESCE(i.item_code,''), i.name_ar,
                   COALESCE(u.name_ar, u.name,''), icl.system_qty, icl.counted_qty
            FROM public.inventory_count_lines icl
            JOIN public.items i ON i.id = icl.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            WHERE icl.count_id = @cid
            ORDER BY i.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", countId);
        using var r = cmd.ExecuteReader();
        var list = new List<InventoryCountLine>();
        while (r.Read())
            list.Add(new InventoryCountLine
            {
                ItemId     = r.GetInt32(0),
                ItemCode   = r.GetString(1),
                ItemName   = r.GetString(2),
                UnitName   = r.GetString(3),
                SystemQty  = r.GetDecimal(4),
                CountedQty = r.GetDecimal(5)
            });
        return list;
    }

    /// <summary>حفظ كميات الجرد (بدون اعتماد بعد)</summary>
    public void SaveCountLines(int countId, List<(int itemId, decimal qty)> lines)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            foreach (var (itemId, qty) in lines)
            {
                const string sql = """
                    UPDATE public.inventory_count_lines
                    SET counted_qty = @qty
                    WHERE count_id = @cid AND item_id = @item
                    """;
                using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("qty",  qty);
                cmd.Parameters.AddWithValue("cid",  countId);
                cmd.Parameters.AddWithValue("item", itemId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>اعتماد الجرد: تحديث الأرصدة الفعلية في stock_levels</summary>
    public void ApproveInventoryCount(int countId, int approvedByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // جلب warehouse_id
            int warehouseId;
            using (var cmd = new NpgsqlCommand("SELECT warehouse_id FROM public.inventory_counts WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", countId);
                warehouseId = (int)cmd.ExecuteScalar()!;
            }

            // تحديث stock_levels من counted_qty
            const string update = """
                INSERT INTO public.stock_levels (item_id, warehouse_id, quantity, last_updated)
                SELECT icl.item_id, @wh, icl.counted_qty, NOW()
                FROM public.inventory_count_lines icl
                WHERE icl.count_id = @cid
                ON CONFLICT (item_id, warehouse_id)
                DO UPDATE SET quantity = EXCLUDED.quantity, last_updated = NOW()
                """;
            using var cmd2 = new NpgsqlCommand(update, conn, tx);
            cmd2.Parameters.AddWithValue("wh",  warehouseId);
            cmd2.Parameters.AddWithValue("cid", countId);
            cmd2.ExecuteNonQuery();

            // تحديث حالة الجرد
            const string approve = """
                UPDATE public.inventory_counts
                SET status = 'approved', approved_by = @by, approved_at = NOW()
                WHERE id = @id
                """;
            using var cmd3 = new NpgsqlCommand(approve, conn, tx);
            cmd3.Parameters.AddWithValue("by", approvedByUserId);
            cmd3.Parameters.AddWithValue("id", countId);
            cmd3.ExecuteNonQuery();

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ تحويلات المستودعات (TASK-020) ════════════════════════

    /// <summary>إنشاء تحويل جديد وإرجاع الـ ID</summary>
    public int CreateTransfer(int fromWh, int toWh, string notes, int createdByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var num = "TRF-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        const string sql = """
            INSERT INTO public.warehouse_transfers
                (transfer_number, from_warehouse, to_warehouse, transfer_date, status, notes, created_by)
            VALUES (@num, @from, @to, @dt, 'draft', @notes, @by)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("num",   num);
        cmd.Parameters.AddWithValue("from",  fromWh);
        cmd.Parameters.AddWithValue("to",    toWh);
        cmd.Parameters.AddWithValue("dt",    DateTime.Today);
        cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("by",    createdByUserId);
        return (int)cmd.ExecuteScalar()!;
    }

    /// <summary>قائمة التحويلات</summary>
    public List<WarehouseTransfer> GetTransfers()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT t.id, t.transfer_number,
                   t.from_warehouse, wf.name,
                   t.to_warehouse,   wt.name,
                   t.transfer_date, t.status,
                   COALESCE(t.notes,''),
                   COALESCE(uc.full_name,''), COALESCE(ua.full_name,'')
            FROM public.warehouse_transfers t
            JOIN public.warehouses wf ON wf.id = t.from_warehouse
            JOIN public.warehouses wt ON wt.id = t.to_warehouse
            LEFT JOIN auth.users uc ON uc.id = t.created_by
            LEFT JOIN auth.users ua ON ua.id = t.approved_by
            ORDER BY t.transfer_date DESC, t.id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<WarehouseTransfer>();
        while (r.Read())
            list.Add(new WarehouseTransfer
            {
                Id             = r.GetInt32(0),
                TransferNumber = r.GetString(1),
                FromWarehouseId= r.GetInt32(2),
                FromWarehouse  = r.GetString(3),
                ToWarehouseId  = r.GetInt32(4),
                ToWarehouse    = r.GetString(5),
                TransferDate   = r.GetDateTime(6),
                Status         = r.GetString(7),
                Notes          = r.GetString(8),
                CreatedByName  = r.GetString(9),
                ApprovedByName = r.GetString(10)
            });
        return list;
    }

    /// <summary>سطور تحويل محدد</summary>
    public List<TransferLine> GetTransferLines(int transferId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT tl.item_id, COALESCE(i.item_code,''), i.name_ar,
                   COALESCE(u.name_ar, u.name,''), tl.quantity,
                   COALESCE(sl.quantity, 0) AS stock_qty
            FROM public.transfer_lines tl
            JOIN public.items i ON i.id = tl.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            LEFT JOIN public.warehouse_transfers t ON t.id = tl.transfer_id
            LEFT JOIN public.stock_levels sl ON sl.item_id = tl.item_id AND sl.warehouse_id = t.from_warehouse
            WHERE tl.transfer_id = @tid
            ORDER BY i.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tid", transferId);
        using var r = cmd.ExecuteReader();
        var list = new List<TransferLine>();
        while (r.Read())
            list.Add(new TransferLine
            {
                ItemId   = r.GetInt32(0),
                ItemCode = r.GetString(1),
                ItemName = r.GetString(2),
                UnitName = r.GetString(3),
                Quantity = r.GetDecimal(4),
                StockQty = r.GetDecimal(5)
            });
        return list;
    }

    /// <summary>حفظ/تحديث سطور التحويل</summary>
    public void SaveTransferLines(int transferId, List<(int itemId, decimal qty)> lines)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // حذف القديم وإعادة إدراج
            using var del = new NpgsqlCommand(
                "DELETE FROM public.transfer_lines WHERE transfer_id = @tid", conn, tx);
            del.Parameters.AddWithValue("tid", transferId);
            del.ExecuteNonQuery();

            foreach (var (itemId, qty) in lines.Where(l => l.qty > 0))
            {
                const string ins = """
                    INSERT INTO public.transfer_lines (transfer_id, item_id, quantity)
                    VALUES (@tid, @item, @qty)
                    """;
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                cmd.Parameters.AddWithValue("tid",  transferId);
                cmd.Parameters.AddWithValue("item", itemId);
                cmd.Parameters.AddWithValue("qty",  qty);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>اعتماد التحويل: خصم من المصدر وإضافة للوجهة</summary>
    public void ApproveTransfer(int transferId, int approvedByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // جلب بيانات التحويل
            int fromWh, toWh;
            using (var cmd = new NpgsqlCommand(
                "SELECT from_warehouse, to_warehouse FROM public.warehouse_transfers WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", transferId);
                using var r = cmd.ExecuteReader();
                r.Read();
                fromWh = r.GetInt32(0);
                toWh   = r.GetInt32(1);
            }

            // جلب السطور
            var lines = new List<(int itemId, decimal qty)>();
            using (var cmd = new NpgsqlCommand(
                "SELECT item_id, quantity FROM public.transfer_lines WHERE transfer_id=@tid", conn, tx))
            {
                cmd.Parameters.AddWithValue("tid", transferId);
                using var r = cmd.ExecuteReader();
                while (r.Read()) lines.Add((r.GetInt32(0), r.GetDecimal(1)));
            }

            // التحقق من الرصيد الكافي
            foreach (var (itemId, qty) in lines)
            {
                const string chk = "SELECT COALESCE(quantity,0) FROM public.stock_levels WHERE item_id=@i AND warehouse_id=@w";
                using var cmd = new NpgsqlCommand(chk, conn, tx);
                cmd.Parameters.AddWithValue("i", itemId);
                cmd.Parameters.AddWithValue("w", fromWh);
                var avail = (decimal)(cmd.ExecuteScalar() ?? 0m);
                if (avail < qty)
                    throw new InvalidOperationException($"الكمية المطلوبة ({qty}) أكبر من المتاح ({avail}) للصنف ID={itemId}");
            }

            // تحديث الأرصدة
            const string upsert = """
                INSERT INTO public.stock_levels (item_id, warehouse_id, quantity, last_updated)
                VALUES (@item, @wh, @qty, NOW())
                ON CONFLICT (item_id, warehouse_id)
                DO UPDATE SET quantity = stock_levels.quantity + @qty, last_updated = NOW()
                """;
            foreach (var (itemId, qty) in lines)
            {
                // خصم من المصدر
                using var cmdF = new NpgsqlCommand(upsert, conn, tx);
                cmdF.Parameters.AddWithValue("item", itemId);
                cmdF.Parameters.AddWithValue("wh",   fromWh);
                cmdF.Parameters.AddWithValue("qty",  -qty);
                cmdF.ExecuteNonQuery();
                // إضافة للوجهة
                using var cmdT = new NpgsqlCommand(upsert, conn, tx);
                cmdT.Parameters.AddWithValue("item", itemId);
                cmdT.Parameters.AddWithValue("wh",   toWh);
                cmdT.Parameters.AddWithValue("qty",  qty);
                cmdT.ExecuteNonQuery();
            }

            // تحديث حالة التحويل
            const string approve = """
                UPDATE public.warehouse_transfers
                SET status='approved', approved_by=@by, approved_at=NOW()
                WHERE id=@id
                """;
            using var cmdA = new NpgsqlCommand(approve, conn, tx);
            cmdA.Parameters.AddWithValue("by", approvedByUserId);
            cmdA.Parameters.AddWithValue("id", transferId);
            cmdA.ExecuteNonQuery();

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ تسجيل التالف (TASK-021) ══════════════════════════════

    /// <summary>إنشاء سجل تالف جديد وإرجاع الـ ID</summary>
    public int CreateDamageRecord(int warehouseId, string reason, string notes, int createdByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var num = "TLF-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        const string sql = """
            INSERT INTO public.damage_records
                (record_number, warehouse_id, record_date, reason, status, notes, created_by)
            VALUES (@num, @wh, @dt, @reason, 'pending', @notes, @by)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("num",    num);
        cmd.Parameters.AddWithValue("wh",     warehouseId);
        cmd.Parameters.AddWithValue("dt",     DateTime.Today);
        cmd.Parameters.AddWithValue("reason", reason);
        cmd.Parameters.AddWithValue("notes",  (object?)notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("by",     createdByUserId);
        return (int)cmd.ExecuteScalar()!;
    }

    /// <summary>قائمة سجلات التالف</summary>
    public List<DamageRecord> GetDamageRecords(int? warehouseId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT d.id, d.record_number, d.warehouse_id, w.name,
                   d.record_date, d.reason, d.status,
                   COALESCE(d.total_value, 0),
                   COALESCE(d.notes,''),
                   COALESCE(uc.full_name,''), COALESCE(ua.full_name,'')
            FROM public.damage_records d
            JOIN public.warehouses w ON w.id = d.warehouse_id
            LEFT JOIN auth.users uc ON uc.id = d.created_by
            LEFT JOIN auth.users ua ON ua.id = d.approved_by
            WHERE 1=1
            """;
        if (warehouseId.HasValue) sql += " AND d.warehouse_id = @wh";
        sql += " ORDER BY d.record_date DESC, d.id DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (warehouseId.HasValue) cmd.Parameters.AddWithValue("wh", warehouseId.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<DamageRecord>();
        while (r.Read())
            list.Add(new DamageRecord
            {
                Id            = r.GetInt32(0),
                RecordNumber  = r.GetString(1),
                WarehouseId   = r.GetInt32(2),
                WarehouseName = r.GetString(3),
                RecordDate    = r.GetDateTime(4),
                Reason        = r.GetString(5),
                Status        = r.GetString(6),
                TotalValue    = r.GetDecimal(7),
                Notes         = r.GetString(8),
                CreatedBy     = r.GetString(9),
                ApprovedBy    = r.GetString(10)
            });
        return list;
    }

    /// <summary>سطور سجل تالف محدد</summary>
    public List<DamageLine> GetDamageLines(int recordId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT dl.item_id, COALESCE(i.item_code,''), i.name_ar,
                   COALESCE(u.name_ar, u.name,''), dl.quantity, dl.unit_cost
            FROM public.damage_lines dl
            JOIN public.items i ON i.id = dl.item_id
            LEFT JOIN public.units u ON u.id = i.unit_id
            WHERE dl.record_id = @rid
            ORDER BY i.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("rid", recordId);
        using var r = cmd.ExecuteReader();
        var list = new List<DamageLine>();
        while (r.Read())
            list.Add(new DamageLine
            {
                ItemId   = r.GetInt32(0),
                ItemCode = r.GetString(1),
                ItemName = r.GetString(2),
                UnitName = r.GetString(3),
                Quantity = r.GetDecimal(4),
                UnitCost = r.GetDecimal(5)
            });
        return list;
    }

    /// <summary>حفظ سطور التالف وتحديث إجمالي القيمة</summary>
    public void SaveDamageLines(int recordId, List<(int itemId, decimal qty, decimal cost)> lines)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            using var del = new NpgsqlCommand(
                "DELETE FROM public.damage_lines WHERE record_id = @rid", conn, tx);
            del.Parameters.AddWithValue("rid", recordId);
            del.ExecuteNonQuery();

            decimal total = 0;
            foreach (var (itemId, qty, cost) in lines.Where(l => l.qty > 0))
            {
                const string ins = """
                    INSERT INTO public.damage_lines (record_id, item_id, quantity, unit_cost)
                    VALUES (@rid, @item, @qty, @cost)
                    """;
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                cmd.Parameters.AddWithValue("rid",  recordId);
                cmd.Parameters.AddWithValue("item", itemId);
                cmd.Parameters.AddWithValue("qty",  qty);
                cmd.Parameters.AddWithValue("cost", cost);
                cmd.ExecuteNonQuery();
                total += qty * cost;
            }

            using var upd = new NpgsqlCommand(
                "UPDATE public.damage_records SET total_value=@tv WHERE id=@id", conn, tx);
            upd.Parameters.AddWithValue("tv", total);
            upd.Parameters.AddWithValue("id", recordId);
            upd.ExecuteNonQuery();

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>اعتماد التالف: خصم من المخزون</summary>
    public void ApproveDamageRecord(int recordId, int approvedByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            int warehouseId;
            using (var cmd = new NpgsqlCommand(
                "SELECT warehouse_id FROM public.damage_records WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", recordId);
                warehouseId = (int)cmd.ExecuteScalar()!;
            }

            // خصم كل صنف من المخزون
            const string deduct = """
                UPDATE public.stock_levels
                SET quantity = GREATEST(quantity - @qty, 0), last_updated = NOW()
                WHERE item_id = @item AND warehouse_id = @wh
                """;
            var lines = GetDamageLines(recordId);
            foreach (var l in lines)
            {
                using var cmd = new NpgsqlCommand(deduct, conn, tx);
                cmd.Parameters.AddWithValue("qty",  l.Quantity);
                cmd.Parameters.AddWithValue("item", l.ItemId);
                cmd.Parameters.AddWithValue("wh",   warehouseId);
                cmd.ExecuteNonQuery();
            }

            const string approve = """
                UPDATE public.damage_records
                SET status='approved', approved_by=@by, approved_at=NOW()
                WHERE id=@id
                """;
            using var cmdA = new NpgsqlCommand(approve, conn, tx);
            cmdA.Parameters.AddWithValue("by", approvedByUserId);
            cmdA.Parameters.AddWithValue("id", recordId);
            cmdA.ExecuteNonQuery();

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    /// <summary>أصناف متاحة في مستودع مع رصيدها</summary>
    public List<StockLevel> GetAvailableStock(int warehouseId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT i.id, COALESCE(i.item_code,''), i.name_ar,
                   COALESCE(g.name_ar, g.name,''),
                   COALESCE(u.name_ar, u.name,''),
                   @wh,
                   COALESCE(sl.quantity, 0),
                   COALESCE(i.reorder_point, 0),
                   COALESCE(i.purchase_price, 0)
            FROM public.items i
            LEFT JOIN public.item_groups g  ON g.id = i.group_id
            LEFT JOIN public.units       u  ON u.id = i.unit_id
            LEFT JOIN public.stock_levels sl ON sl.item_id = i.id AND sl.warehouse_id = @wh
            WHERE i.is_active = true AND COALESCE(sl.quantity, 0) > 0
            ORDER BY i.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("wh", warehouseId);
        using var r = cmd.ExecuteReader();
        var list = new List<StockLevel>();
        while (r.Read())
            list.Add(new StockLevel
            {
                ItemId        = r.GetInt32(0),
                ItemCode      = r.GetString(1),
                ItemName      = r.GetString(2),
                GroupName     = r.GetString(3),
                UnitName      = r.GetString(4),
                WarehouseId   = r.GetInt32(5),
                Quantity      = r.GetDecimal(6),
                ReorderPoint  = r.GetDecimal(7),
                PurchasePrice = r.GetDecimal(8)
            });
        return list;
    }
}
