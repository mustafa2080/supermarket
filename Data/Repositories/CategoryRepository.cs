using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>
/// TASK-010 — Repository لمجموعات الأصناف وأنواعها (CRUD كامل)
/// </summary>
internal class CategoryRepository
{
    // ══════════════════════════════════════════════════════════
    //  مجموعات الأصناف — item_groups
    // ══════════════════════════════════════════════════════════

    public List<ItemGroup> GetAllGroups()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT g.id, g.name, g.name_ar, g.is_active,
                   COUNT(i.id) AS item_count
            FROM public.item_groups g
            LEFT JOIN public.items i ON i.group_id = g.id AND i.is_active = TRUE
            GROUP BY g.id, g.name, g.name_ar, g.is_active
            ORDER BY g.name_ar
            """;
        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<ItemGroup>();
        while (reader.Read())
            list.Add(MapGroup(reader));
        return list;
    }

    public int InsertGroup(ItemGroup g)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.item_groups (name, name_ar, is_active)
            VALUES (@name, @nameAr, @active)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",   g.Name);
        cmd.Parameters.AddWithValue("nameAr", g.NameAr);
        cmd.Parameters.AddWithValue("active", g.IsActive);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdateGroup(ItemGroup g)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.item_groups
               SET name = @name, name_ar = @nameAr, is_active = @active
             WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",   g.Name);
        cmd.Parameters.AddWithValue("nameAr", g.NameAr);
        cmd.Parameters.AddWithValue("active", g.IsActive);
        cmd.Parameters.AddWithValue("id",     g.Id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>حذف مجموعة — يرفض لو فيها أصناف مرتبطة</summary>
    public (bool ok, string error) DeleteGroup(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();

        // فحص الأصناف المرتبطة
        using var chk = new NpgsqlCommand(
            "SELECT COUNT(1) FROM public.items WHERE group_id = @id", conn);
        chk.Parameters.AddWithValue("id", id);
        var count = Convert.ToInt32(chk.ExecuteScalar());
        if (count > 0)
            return (false, $"لا يمكن الحذف — المجموعة تحتوي {count} صنف مرتبط.");

        using var del = new NpgsqlCommand(
            "DELETE FROM public.item_groups WHERE id = @id", conn);
        del.Parameters.AddWithValue("id", id);
        del.ExecuteNonQuery();
        return (true, string.Empty);
    }

    public bool GroupNameExists(string nameAr, int excludeId = 0)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COUNT(1) FROM public.item_groups
            WHERE name_ar = @nameAr AND id <> @excludeId
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nameAr",    nameAr);
        cmd.Parameters.AddWithValue("excludeId", excludeId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static ItemGroup MapGroup(NpgsqlDataReader r) => new()
    {
        Id        = r.GetInt32(0),
        Name      = r.GetString(1),
        NameAr    = r.GetString(2),
        IsActive  = r.GetBoolean(3),
        ItemCount = Convert.ToInt32(r[4])
    };

    // ══════════════════════════════════════════════════════════
    //  أنواع الأصناف — item_types
    // ══════════════════════════════════════════════════════════

    public List<ItemType> GetAllTypes()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT t.id, t.name, t.name_ar, t.is_active,
                   COUNT(i.id) AS item_count
            FROM public.item_types t
            LEFT JOIN public.items i ON i.type_id = t.id AND i.is_active = TRUE
            GROUP BY t.id, t.name, t.name_ar, t.is_active
            ORDER BY t.name_ar
            """;
        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<ItemType>();
        while (reader.Read())
            list.Add(MapType(reader));
        return list;
    }

    public int InsertType(ItemType t)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.item_types (name, name_ar, is_active)
            VALUES (@name, @nameAr, @active)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",   t.Name);
        cmd.Parameters.AddWithValue("nameAr", t.NameAr);
        cmd.Parameters.AddWithValue("active", t.IsActive);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdateType(ItemType t)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.item_types
               SET name = @name, name_ar = @nameAr, is_active = @active
             WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",   t.Name);
        cmd.Parameters.AddWithValue("nameAr", t.NameAr);
        cmd.Parameters.AddWithValue("active", t.IsActive);
        cmd.Parameters.AddWithValue("id",     t.Id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>حذف نوع — يرفض لو فيه أصناف مرتبطة</summary>
    public (bool ok, string error) DeleteType(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();

        using var chk = new NpgsqlCommand(
            "SELECT COUNT(1) FROM public.items WHERE type_id = @id", conn);
        chk.Parameters.AddWithValue("id", id);
        var count = Convert.ToInt32(chk.ExecuteScalar());
        if (count > 0)
            return (false, $"لا يمكن الحذف — النوع مرتبط بـ {count} صنف.");

        using var del = new NpgsqlCommand(
            "DELETE FROM public.item_types WHERE id = @id", conn);
        del.Parameters.AddWithValue("id", id);
        del.ExecuteNonQuery();
        return (true, string.Empty);
    }

    public bool TypeNameExists(string nameAr, int excludeId = 0)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COUNT(1) FROM public.item_types
            WHERE name_ar = @nameAr AND id <> @excludeId
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nameAr",    nameAr);
        cmd.Parameters.AddWithValue("excludeId", excludeId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static ItemType MapType(NpgsqlDataReader r) => new()
    {
        Id        = r.GetInt32(0),
        Name      = r.GetString(1),
        NameAr    = r.GetString(2),
        IsActive  = r.GetBoolean(3),
        ItemCount = Convert.ToInt32(r[4])
    };
}
