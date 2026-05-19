using Npgsql;
using supermarket.Models;
using supermarket.Services;

namespace supermarket.Data.Repositories;

/// <summary>Repository للتسعير — تاريخ الأسعار + التحديث الجماعي — TASK-024</summary>
internal class PricingRepository
{
    // ══ تاريخ الأسعار ════════════════════════════════════════

    public List<PriceHistoryEntry> GetHistory(int? itemId = null, string? priceType = null,
                                               DateTime? from = null, DateTime? to = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT ph.id, ph.item_id, i.item_code, i.name_ar,
                   ph.price_type, ph.old_price, ph.new_price,
                   COALESCE(u.full_name, u.username) AS changed_by,
                   ph.changed_at, COALESCE(ph.reason,'') AS reason
            FROM public.price_history ph
            JOIN public.items i ON i.id = ph.item_id
            LEFT JOIN auth.users u ON u.id = ph.changed_by
            WHERE 1=1
            """;
        var conditions = new List<string>();
        if (itemId.HasValue)  conditions.Add($"ph.item_id = {itemId}");
        if (!string.IsNullOrEmpty(priceType)) conditions.Add($"ph.price_type = '{priceType}'");
        if (from.HasValue) conditions.Add($"ph.changed_at >= '{from:yyyy-MM-dd}'");
        if (to.HasValue)   conditions.Add($"ph.changed_at <  '{to.Value.AddDays(1):yyyy-MM-dd}'");
        if (conditions.Count > 0) sql += " AND " + string.Join(" AND ", conditions);
        sql += " ORDER BY ph.changed_at DESC LIMIT 2000";

        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<PriceHistoryEntry>();
        while (reader.Read())
            list.Add(new PriceHistoryEntry
            {
                Id         = reader.GetInt32(0),
                ItemId     = reader.GetInt32(1),
                ItemCode   = reader.GetString(2),
                ItemName   = reader.GetString(3),
                PriceType  = reader.GetString(4),
                OldPrice   = reader.GetDecimal(5),
                NewPrice   = reader.GetDecimal(6),
                ChangedBy  = reader.IsDBNull(7) ? "—" : reader.GetString(7),
                ChangedAt  = reader.GetDateTime(8),
                Reason     = reader.GetString(9)
            });
        return list;
    }

    // ══ تحديث سعر صنف واحد ═══════════════════════════════════

    /// <summary>يُحدّث سعراً واحداً ويُسجّل في price_history تلقائياً</summary>
    public void UpdateItemPrice(int itemId, string priceType, decimal newPrice, string reason = "")
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            // اقرأ السعر القديم
            var col = priceType switch
            {
                "retail"    => "retail_price",
                "wholesale" => "wholesale_price",
                "purchase"  => "purchase_price",
                _           => "retail_price"
            };
            decimal oldPrice;
            using (var cmd = new NpgsqlCommand(
                $"SELECT {col} FROM public.items WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", itemId);
                oldPrice = (decimal)(cmd.ExecuteScalar() ?? 0m);
            }

            if (oldPrice == newPrice) { tx.Rollback(); return; }

            // حدّث السعر
            using (var cmd = new NpgsqlCommand(
                $"UPDATE public.items SET {col}=@p, updated_at=NOW() WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("p",  newPrice);
                cmd.Parameters.AddWithValue("id", itemId);
                cmd.ExecuteNonQuery();
            }

            // سجّل في التاريخ
            LogPriceChange(conn, tx, itemId, priceType, oldPrice, newPrice, reason);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ معاينة التحديث الجماعي ════════════════════════════════

    public List<BulkPricePreviewRow> PreviewBulkUpdate(BulkPriceUpdateRequest req)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var col = PriceColumn(req.PriceType);
        var sql = $"""
            SELECT i.id, i.item_code, i.name_ar,
                   COALESCE(ig.name_ar,'') AS group_name,
                   COALESCE(s.name,'')     AS supplier_name,
                   i.{col}                AS current_price
            FROM public.items i
            LEFT JOIN public.item_groups ig ON ig.id = i.group_id
            LEFT JOIN public.suppliers    s ON s.id  = i.supplier_id
            WHERE i.is_active = TRUE
            """;
        if (req.GroupId.HasValue)    sql += $" AND i.group_id    = {req.GroupId}";
        if (req.SupplierId.HasValue) sql += $" AND i.supplier_id = {req.SupplierId}";
        sql += " ORDER BY i.name_ar";

        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var rows = new List<BulkPricePreviewRow>();
        while (reader.Read())
        {
            var cur = reader.GetDecimal(5);
            rows.Add(new BulkPricePreviewRow
            {
                ItemId       = reader.GetInt32(0),
                ItemCode     = reader.GetString(1),
                ItemName     = reader.GetString(2),
                GroupName    = reader.GetString(3),
                SupplierName = reader.GetString(4),
                CurrentPrice = cur,
                NewPrice     = CalcNewPrice(cur, req.Method, req.Value)
            });
        }
        return rows;
    }

    // ══ تطبيق التحديث الجماعي ════════════════════════════════

    /// <returns>عدد الأصناف التي تغير سعرها فعلاً</returns>
    public int ApplyBulkUpdate(BulkPriceUpdateRequest req)
    {
        if (req.ItemIds.Count == 0) return 0;
        var col = PriceColumn(req.PriceType);
        int count = 0;

        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            foreach (var itemId in req.ItemIds)
            {
                decimal oldPrice;
                using (var cmd = new NpgsqlCommand(
                    $"SELECT {col} FROM public.items WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", itemId);
                    oldPrice = (decimal)(cmd.ExecuteScalar() ?? 0m);
                }

                var newPrice = CalcNewPrice(oldPrice, req.Method, req.Value);
                if (newPrice == oldPrice) continue;

                using (var cmd = new NpgsqlCommand(
                    $"UPDATE public.items SET {col}=@p, updated_at=NOW() WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("p",  newPrice);
                    cmd.Parameters.AddWithValue("id", itemId);
                    cmd.ExecuteNonQuery();
                }
                LogPriceChange(conn, tx, itemId, req.PriceType, oldPrice, newPrice, req.Reason);
                count++;
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
        return count;
    }

    // ── helpers ───────────────────────────────────────────────

    private static string PriceColumn(string priceType) => priceType switch
    {
        "retail"    => "retail_price",
        "wholesale" => "wholesale_price",
        "purchase"  => "purchase_price",
        _           => "retail_price"
    };

    private static decimal CalcNewPrice(decimal cur, string method, decimal value)
    {
        decimal result = method switch
        {
            "percent_up"   => cur * (1 + value / 100),
            "percent_down" => cur * (1 - value / 100),
            "amount_up"    => cur + value,
            "amount_down"  => cur - value,
            "fixed"        => value,
            _              => cur
        };
        return Math.Max(0, Math.Round(result, 2));
    }

    private static void LogPriceChange(NpgsqlConnection conn, NpgsqlTransaction tx,
        int itemId, string priceType, decimal oldPrice, decimal newPrice, string reason)
    {
        using var cmd = new NpgsqlCommand("""
            INSERT INTO public.price_history
                (item_id, price_type, old_price, new_price, changed_by, reason)
            VALUES (@item, @type, @old, @new, @by, @reason)
            """, conn, tx);
        cmd.Parameters.AddWithValue("item",   itemId);
        cmd.Parameters.AddWithValue("type",   priceType);
        cmd.Parameters.AddWithValue("old",    oldPrice);
        cmd.Parameters.AddWithValue("new",    newPrice);
        cmd.Parameters.AddWithValue("by",     (object?)SessionContext.CurrentUser?.Id ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reason", string.IsNullOrEmpty(reason) ? DBNull.Value : (object)reason);
        cmd.ExecuteNonQuery();
    }
}
