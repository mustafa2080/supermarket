namespace supermarket.Models;

// ── مستوى المخزون لصنف في مستودع ────────────────────────────
public class StockLevel
{
    public int     ItemId       { get; set; }
    public string  ItemCode     { get; set; } = string.Empty;
    public string  ItemName     { get; set; } = string.Empty;
    public string  GroupName    { get; set; } = string.Empty;
    public string  UnitName     { get; set; } = string.Empty;
    public int     WarehouseId  { get; set; }
    public string  WarehouseName{ get; set; } = string.Empty;
    public decimal Quantity     { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal PurchasePrice{ get; set; }
    public decimal StockValue   => Quantity * PurchasePrice;

    public string StockStatus =>
        Quantity <= 0               ? "نفاد" :
        Quantity <= ReorderPoint    ? "منخفض" : "كافي";

    public string StockStatusIcon =>
        Quantity <= 0               ? "❌" :
        Quantity <= ReorderPoint    ? "⚠️" : "✅";
}

// ── تحويل مستودع ─────────────────────────────────────────────
public class WarehouseTransfer
{
    public int      Id              { get; set; }
    public string   TransferNumber  { get; set; } = string.Empty;
    public int      FromWarehouseId { get; set; }
    public string   FromWarehouse   { get; set; } = string.Empty;
    public int      ToWarehouseId   { get; set; }
    public string   ToWarehouse     { get; set; } = string.Empty;
    public DateTime TransferDate    { get; set; } = DateTime.Today;
    public string   Status          { get; set; } = "draft"; // draft | approved
    public string   Notes           { get; set; } = string.Empty;
    public string   CreatedByName   { get; set; } = string.Empty;
    public string   ApprovedByName  { get; set; } = string.Empty;
    public List<TransferLine> Lines { get; set; } = new();

    public string StatusAr => Status == "approved" ? "✅ معتمد" : "📝 مسودة";
}

public class TransferLine
{
    public int     ItemId    { get; set; }
    public string  ItemCode  { get; set; } = string.Empty;
    public string  ItemName  { get; set; } = string.Empty;
    public string  UnitName  { get; set; } = string.Empty;
    public decimal Quantity  { get; set; }
    public decimal StockQty  { get; set; } // الرصيد المتاح في المستودع المصدر
}

// ── جرد المخزون ──────────────────────────────────────────────
public class InventoryCount
{
    public int      Id           { get; set; }
    public string   CountNumber  { get; set; } = string.Empty;
    public int      WarehouseId  { get; set; }
    public string   WarehouseName{ get; set; } = string.Empty;
    public DateTime CountDate    { get; set; } = DateTime.Today;
    public string   Status       { get; set; } = "in_progress";
    public string   Notes        { get; set; } = string.Empty;
    public string   CreatedBy    { get; set; } = string.Empty;
    public string   ApprovedBy   { get; set; } = string.Empty;
    public List<InventoryCountLine> Lines { get; set; } = new();

    public string StatusAr => Status == "approved" ? "✅ معتمد" : "⏳ جاري";
}

public class InventoryCountLine
{
    public int     ItemId     { get; set; }
    public string  ItemCode   { get; set; } = string.Empty;
    public string  ItemName   { get; set; } = string.Empty;
    public string  UnitName   { get; set; } = string.Empty;
    public decimal SystemQty  { get; set; }
    public decimal CountedQty { get; set; }
    public decimal Difference => CountedQty - SystemQty;
}

// ── تسجيل التالف ─────────────────────────────────────────────
public class DamageRecord
{
    public int      Id           { get; set; }
    public string   RecordNumber { get; set; } = string.Empty;
    public int      WarehouseId  { get; set; }
    public string   WarehouseName{ get; set; } = string.Empty;
    public DateTime RecordDate   { get; set; } = DateTime.Today;
    public string   Reason       { get; set; } = "damage"; // damage | expiry | theft
    public decimal  TotalValue   { get; set; }
    public string   Status       { get; set; } = "pending"; // pending | approved
    public string   Notes        { get; set; } = string.Empty;
    public string   CreatedBy    { get; set; } = string.Empty;
    public string   ApprovedBy   { get; set; } = string.Empty;
    public List<DamageLine> Lines { get; set; } = new();

    public string ReasonAr => Reason switch
    {
        "damage" => "تلف",
        "expiry" => "انتهاء صلاحية",
        "theft"  => "سرقة",
        _        => Reason
    };
    public string StatusAr => Status == "approved" ? "✅ معتمد" : "⏳ انتظار اعتماد";
}

public class DamageLine
{
    public int     ItemId     { get; set; }
    public string  ItemCode   { get; set; } = string.Empty;
    public string  ItemName   { get; set; } = string.Empty;
    public string  UnitName   { get; set; } = string.Empty;
    public decimal Quantity   { get; set; }
    public decimal UnitCost   { get; set; }
    public decimal TotalValue => Quantity * UnitCost;
}
