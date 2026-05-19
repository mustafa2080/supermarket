using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>
/// Repository للأصناف — CRUD + بحث + استيراد
/// </summary>
internal class ItemRepository
{
    // ── قراءة ─────────────────────────────────────────────────
    public List<Item> GetAll(bool activeOnly = true)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT i.id, i.item_code, i.name_ar, i.name_en, i.barcode, i.barcode_type,
                   i.group_id,  ig.name_ar  AS group_name,
                   i.type_id,   it.name_ar  AS type_name,
                   i.unit_id,   u.name_ar   AS unit_name,
                   i.supplier_id, s.name    AS supplier_name,
                   i.purchase_price, i.retail_price, i.wholesale_price,
                   i.tax_rate, i.reorder_point, i.image_path,
                   i.is_active, i.notes, i.created_at,
                   COALESCE(sl.quantity, 0) AS current_stock
            FROM public.items i
            LEFT JOIN public.item_groups ig ON i.group_id   = ig.id
            LEFT JOIN public.item_types  it ON i.type_id    = it.id
            LEFT JOIN public.units        u ON i.unit_id     = u.id
            LEFT JOIN public.suppliers    s ON i.supplier_id = s.id
            LEFT JOIN public.stock_levels sl
                   ON sl.item_id = i.id
                   AND sl.warehouse_id = (SELECT id FROM public.warehouses WHERE is_default = TRUE LIMIT 1)
            """ + (activeOnly ? "WHERE i.is_active = TRUE " : "") + """
            ORDER BY i.name_ar
            """;
        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<Item>();
        while (reader.Read()) list.Add(MapItem(reader));
        return list;
    }

    public Item? GetByBarcode(string barcode)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT i.id, i.item_code, i.name_ar, i.name_en, i.barcode, i.barcode_type,
                   i.group_id, ig.name_ar AS group_name,
                   i.type_id,  it.name_ar AS type_name,
                   i.unit_id,   u.name_ar AS unit_name,
                   i.supplier_id, s.name  AS supplier_name,
                   i.purchase_price, i.retail_price, i.wholesale_price,
                   i.tax_rate, i.reorder_point, i.image_path,
                   i.is_active, i.notes, i.created_at,
                   COALESCE(sl.quantity, 0) AS current_stock
            FROM public.items i
            LEFT JOIN public.item_groups ig ON i.group_id   = ig.id
            LEFT JOIN public.item_types  it ON i.type_id    = it.id
            LEFT JOIN public.units        u ON i.unit_id     = u.id
            LEFT JOIN public.suppliers    s ON i.supplier_id = s.id
            LEFT JOIN public.stock_levels sl
                   ON sl.item_id = i.id
                   AND sl.warehouse_id = (SELECT id FROM public.warehouses WHERE is_default = TRUE LIMIT 1)
            WHERE i.barcode = @barcode AND i.is_active = TRUE
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("barcode", barcode);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapItem(reader) : null;
    }

    public List<Item> Search(string query, int? groupId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT i.id, i.item_code, i.name_ar, i.name_en, i.barcode, i.barcode_type,
                   i.group_id, ig.name_ar AS group_name,
                   i.type_id,  it.name_ar AS type_name,
                   i.unit_id,   u.name_ar AS unit_name,
                   i.supplier_id, s.name  AS supplier_name,
                   i.purchase_price, i.retail_price, i.wholesale_price,
                   i.tax_rate, i.reorder_point, i.image_path,
                   i.is_active, i.notes, i.created_at,
                   COALESCE(sl.quantity, 0) AS current_stock
            FROM public.items i
            LEFT JOIN public.item_groups ig ON i.group_id   = ig.id
            LEFT JOIN public.item_types  it ON i.type_id    = it.id
            LEFT JOIN public.units        u ON i.unit_id     = u.id
            LEFT JOIN public.suppliers    s ON i.supplier_id = s.id
            LEFT JOIN public.stock_levels sl
                   ON sl.item_id = i.id
                   AND sl.warehouse_id = (SELECT id FROM public.warehouses WHERE is_default = TRUE LIMIT 1)
            WHERE i.is_active = TRUE
              AND (i.name_ar ILIKE @q OR i.barcode ILIKE @q OR i.item_code ILIKE @q)
              AND (@groupId IS NULL OR i.group_id = @groupId)
            ORDER BY i.name_ar
            LIMIT 100
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q",       $"%{query}%");
        cmd.Parameters.AddWithValue("groupId", groupId.HasValue ? (object)groupId.Value : DBNull.Value);
        using var reader = cmd.ExecuteReader();
        var list = new List<Item>();
        while (reader.Read()) list.Add(MapItem(reader));
        return list;
    }

    public bool BarcodeExists(string barcode, int excludeId = 0)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM public.items WHERE barcode = @barcode AND id <> @excludeId";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("barcode",   barcode);
        cmd.Parameters.AddWithValue("excludeId", excludeId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public string NextItemCode()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(item_code FROM 'ITEM-(\d+)') AS INT)), 0) + 1
            FROM public.items WHERE item_code ~ '^ITEM-\d+$'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        var seq = Convert.ToInt32(cmd.ExecuteScalar());
        return $"ITEM-{seq:0000}";
    }

    // ── CRUD ──────────────────────────────────────────────────
    public int Insert(Item item, int createdBy)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.items
                (item_code, name_ar, name_en, barcode, barcode_type,
                 group_id, type_id, unit_id, supplier_id,
                 purchase_price, retail_price, wholesale_price,
                 tax_rate, reorder_point, image_path, is_active, notes, created_by)
            VALUES
                (@code, @nameAr, @nameEn, @barcode, @barcodeType,
                 @groupId, @typeId, @unitId, @supplierId,
                 @purchasePrice, @retailPrice, @wholesalePrice,
                 @taxRate, @reorderPoint, @imagePath, @active, @notes, @createdBy)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code",         item.ItemCode);
        cmd.Parameters.AddWithValue("nameAr",       item.NameAr);
        cmd.Parameters.AddWithValue("nameEn",       (object?)item.NameEn       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("barcode",      (object?)item.Barcode      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("barcodeType",  item.BarcodeType);
        cmd.Parameters.AddWithValue("groupId",      (object?)item.GroupId      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("typeId",       (object?)item.TypeId       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("unitId",       (object?)item.UnitId       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("supplierId",   (object?)item.SupplierId   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("purchasePrice",item.PurchasePrice);
        cmd.Parameters.AddWithValue("retailPrice",  item.RetailPrice);
        cmd.Parameters.AddWithValue("wholesalePrice",item.WholesalePrice);
        cmd.Parameters.AddWithValue("taxRate",      item.TaxRate);
        cmd.Parameters.AddWithValue("reorderPoint", item.ReorderPoint);
        cmd.Parameters.AddWithValue("imagePath",    (object?)item.ImagePath    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("active",       item.IsActive);
        cmd.Parameters.AddWithValue("notes",        (object?)item.Notes        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdBy",    createdBy);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void Update(Item item)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.items SET
                name_ar = @nameAr, name_en = @nameEn, barcode = @barcode,
                group_id = @groupId, type_id = @typeId, unit_id = @unitId,
                supplier_id = @supplierId, purchase_price = @purchasePrice,
                retail_price = @retailPrice, wholesale_price = @wholesalePrice,
                tax_rate = @taxRate, reorder_point = @reorderPoint,
                image_path = @imagePath, is_active = @active,
                notes = @notes, updated_at = NOW()
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nameAr",        item.NameAr);
        cmd.Parameters.AddWithValue("nameEn",        (object?)item.NameEn      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("barcode",       (object?)item.Barcode     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("groupId",       (object?)item.GroupId     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("typeId",        (object?)item.TypeId      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("unitId",        (object?)item.UnitId      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("supplierId",    (object?)item.SupplierId  ?? DBNull.Value);
        cmd.Parameters.AddWithValue("purchasePrice", item.PurchasePrice);
        cmd.Parameters.AddWithValue("retailPrice",   item.RetailPrice);
        cmd.Parameters.AddWithValue("wholesalePrice",item.WholesalePrice);
        cmd.Parameters.AddWithValue("taxRate",       item.TaxRate);
        cmd.Parameters.AddWithValue("reorderPoint",  item.ReorderPoint);
        cmd.Parameters.AddWithValue("imagePath",     (object?)item.ImagePath   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("active",        item.IsActive);
        cmd.Parameters.AddWithValue("notes",         (object?)item.Notes       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id",            item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Lookup Lists ───────────────────────────────────────────
    public List<ItemGroup> GetGroups()
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd  = new NpgsqlCommand(
            "SELECT id, name, name_ar FROM public.item_groups WHERE is_active = TRUE ORDER BY name_ar", conn);
        using var r = cmd.ExecuteReader();
        var list = new List<ItemGroup>();
        while (r.Read()) list.Add(new ItemGroup { Id = r.GetInt32(0), Name = r.GetString(1), NameAr = r.GetString(2) });
        return list;
    }

    public List<Unit> GetUnits()
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd  = new NpgsqlCommand(
            "SELECT id, name, name_ar, COALESCE(symbol,'') FROM public.units ORDER BY name_ar", conn);
        using var r = cmd.ExecuteReader();
        var list = new List<Unit>();
        while (r.Read()) list.Add(new Unit { Id = r.GetInt32(0), Name = r.GetString(1), NameAr = r.GetString(2), Symbol = r.GetString(3) });
        return list;
    }

    // ── Mapping ───────────────────────────────────────────────
    private static Item MapItem(NpgsqlDataReader r) => new()
    {
        Id             = r.GetInt32(r.GetOrdinal("id")),
        ItemCode       = r.GetString(r.GetOrdinal("item_code")),
        NameAr         = r.GetString(r.GetOrdinal("name_ar")),
        NameEn         = r.IsDBNull(r.GetOrdinal("name_en"))       ? "" : r.GetString(r.GetOrdinal("name_en")),
        Barcode        = r.IsDBNull(r.GetOrdinal("barcode"))        ? "" : r.GetString(r.GetOrdinal("barcode")),
        BarcodeType    = r.GetString(r.GetOrdinal("barcode_type")),
        GroupId        = r.IsDBNull(r.GetOrdinal("group_id"))       ? null : r.GetInt32(r.GetOrdinal("group_id")),
        GroupName      = r.IsDBNull(r.GetOrdinal("group_name"))     ? "" : r.GetString(r.GetOrdinal("group_name")),
        TypeId         = r.IsDBNull(r.GetOrdinal("type_id"))        ? null : r.GetInt32(r.GetOrdinal("type_id")),
        TypeName       = r.IsDBNull(r.GetOrdinal("type_name"))      ? "" : r.GetString(r.GetOrdinal("type_name")),
        UnitId         = r.IsDBNull(r.GetOrdinal("unit_id"))        ? null : r.GetInt32(r.GetOrdinal("unit_id")),
        UnitName       = r.IsDBNull(r.GetOrdinal("unit_name"))      ? "" : r.GetString(r.GetOrdinal("unit_name")),
        SupplierId     = r.IsDBNull(r.GetOrdinal("supplier_id"))    ? null : r.GetInt32(r.GetOrdinal("supplier_id")),
        SupplierName   = r.IsDBNull(r.GetOrdinal("supplier_name"))  ? "" : r.GetString(r.GetOrdinal("supplier_name")),
        PurchasePrice  = r.GetDecimal(r.GetOrdinal("purchase_price")),
        RetailPrice    = r.GetDecimal(r.GetOrdinal("retail_price")),
        WholesalePrice = r.GetDecimal(r.GetOrdinal("wholesale_price")),
        TaxRate        = r.GetDecimal(r.GetOrdinal("tax_rate")),
        ReorderPoint   = r.GetDecimal(r.GetOrdinal("reorder_point")),
        ImagePath      = r.IsDBNull(r.GetOrdinal("image_path"))     ? null : r.GetString(r.GetOrdinal("image_path")),
        IsActive       = r.GetBoolean(r.GetOrdinal("is_active")),
        Notes          = r.IsDBNull(r.GetOrdinal("notes"))          ? "" : r.GetString(r.GetOrdinal("notes")),
        CreatedAt      = r.GetDateTime(r.GetOrdinal("created_at")),
        CurrentStock   = r.GetDecimal(r.GetOrdinal("current_stock"))
    };
}
