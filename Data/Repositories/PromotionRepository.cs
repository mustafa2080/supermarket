using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للعروض والخصومات — TASK-016</summary>
internal class PromotionRepository
{
    // ── قائمة كل العروض ──────────────────────────────────────
    public List<Promotion> GetAll(bool? activeOnly = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT p.id, p.name, p.type,
                   p.discount_value, p.buy_quantity, p.get_quantity, p.get_price,
                   p.applies_to,
                   p.item_id,  COALESCE(i.name_ar, ''),
                   p.group_id, COALESCE(g.name_ar, ''),
                   p.start_date, p.end_date, p.is_active
            FROM public.promotions p
            LEFT JOIN public.items i       ON i.id = p.item_id
            LEFT JOIN public.item_groups g ON g.id = p.group_id
            ORDER BY p.end_date DESC, p.id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Promotion>();
        while (r.Read())
        {
            var pr = MapPromotion(r);
            if (activeOnly == null || pr.IsActive == activeOnly.Value)
                list.Add(pr);
        }
        return list;
    }

    // ── العروض النشطة اليوم (للـ POS) ───────────────────────
    public List<Promotion> GetActive()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT p.id, p.name, p.type,
                   p.discount_value, p.buy_quantity, p.get_quantity, p.get_price,
                   p.applies_to,
                   p.item_id,  COALESCE(i.name_ar, ''),
                   p.group_id, COALESCE(g.name_ar, ''),
                   p.start_date, p.end_date, p.is_active
            FROM public.promotions p
            LEFT JOIN public.items i       ON i.id = p.item_id
            LEFT JOIN public.item_groups g ON g.id = p.group_id
            WHERE p.is_active = TRUE
              AND p.start_date <= CURRENT_DATE
              AND p.end_date   >= CURRENT_DATE
            ORDER BY p.id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Promotion>();
        while (r.Read()) list.Add(MapPromotion(r));
        return list;
    }

    // ── إضافة عرض ────────────────────────────────────────────
    public int Insert(Promotion p)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.promotions
                (name, type, discount_value, buy_quantity, get_quantity, get_price,
                 applies_to, item_id, group_id, start_date, end_date, is_active)
            VALUES (@name, @type, @disc, @buyQ, @getQ, @getP,
                    @appTo, @item, @grp, @start, @end, @active)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        AddParams(cmd, p);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    // ── تعديل عرض ────────────────────────────────────────────
    public void Update(Promotion p)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.promotions SET
                name=@name, type=@type, discount_value=@disc,
                buy_quantity=@buyQ, get_quantity=@getQ, get_price=@getP,
                applies_to=@appTo, item_id=@item, group_id=@grp,
                start_date=@start, end_date=@end, is_active=@active
            WHERE id=@id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        AddParams(cmd, p);
        cmd.Parameters.AddWithValue("id", p.Id);
        cmd.ExecuteNonQuery();
    }

    // ── تفعيل / تعطيل ────────────────────────────────────────
    public void SetActive(int id, bool active)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd  = new NpgsqlCommand(
            "UPDATE public.promotions SET is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("a",  active);
        cmd.Parameters.AddWithValue("id", id);
        cmd.ExecuteNonQuery();
    }

    // ── helpers ───────────────────────────────────────────────
    private static void AddParams(NpgsqlCommand cmd, Promotion p)
    {
        cmd.Parameters.AddWithValue("name",  p.Name);
        cmd.Parameters.AddWithValue("type",  p.Type);
        cmd.Parameters.AddWithValue("disc",  (object?)p.DiscountValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("buyQ",  (object?)p.BuyQuantity   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("getQ",  (object?)p.GetQuantity   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("getP",  (object?)p.GetPrice      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("appTo", p.AppliesTo);
        cmd.Parameters.AddWithValue("item",  (object?)p.ItemId        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("grp",   (object?)p.GroupId       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("start", p.StartDate);
        cmd.Parameters.AddWithValue("end",   p.EndDate);
        cmd.Parameters.AddWithValue("active",p.IsActive);
    }

    private static Promotion MapPromotion(NpgsqlDataReader r) => new()
    {
        Id            = r.GetInt32(0),
        Name          = r.GetString(1),
        Type          = r.GetString(2),
        DiscountValue = r.IsDBNull(3)  ? null : r.GetDecimal(3),
        BuyQuantity   = r.IsDBNull(4)  ? null : r.GetInt32(4),
        GetQuantity   = r.IsDBNull(5)  ? null : r.GetInt32(5),
        GetPrice      = r.IsDBNull(6)  ? null : r.GetDecimal(6),
        AppliesTo     = r.GetString(7),
        ItemId        = r.IsDBNull(8)  ? null : r.GetInt32(8),
        ItemName      = r.GetString(9),
        GroupId       = r.IsDBNull(10) ? null : r.GetInt32(11),
        GroupName     = r.GetString(11),
        StartDate     = r.GetDateTime(12),
        EndDate       = r.GetDateTime(13),
        IsActive      = r.GetBoolean(14)
    };
}
