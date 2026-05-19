namespace supermarket.Models;

// ── مرتجع المشتريات ──────────────────────────────────────────
public class PurchaseReturn
{
    public int      Id                 { get; set; }
    public string   ReturnNumber       { get; set; } = string.Empty;
    public int?     OriginalInvoiceId  { get; set; }
    public string   OriginalInvoiceNum { get; set; } = string.Empty;  // للعرض فقط
    public int      SupplierId         { get; set; }
    public string   SupplierName       { get; set; } = string.Empty;
    public int      WarehouseId        { get; set; }
    public string   WarehouseName      { get; set; } = string.Empty;
    public DateTime ReturnDate         { get; set; } = DateTime.Today;
    public decimal  TotalAmount        { get; set; }
    public string   Status             { get; set; } = "draft"; // draft | approved
    public string   Notes              { get; set; } = string.Empty;
    public int?     CreatedBy          { get; set; }
    public DateTime CreatedAt          { get; set; }
    public List<PurchaseReturnLine> Lines { get; set; } = new();

    public string StatusAr => Status switch
    {
        "approved" => "✅ معتمد",
        "draft"    => "📝 مسودة",
        _          => Status
    };
}

// ── سطر مرتجع المشتريات ─────────────────────────────────────
public class PurchaseReturnLine
{
    public int     Id           { get; set; }
    public int     ReturnId     { get; set; }
    public int     ItemId       { get; set; }
    public string  ItemCode     { get; set; } = string.Empty;
    public string  ItemName     { get; set; } = string.Empty;
    public string  UnitName     { get; set; } = string.Empty;
    public decimal MaxQty       { get; set; }   // الكمية المشتراة الأصلية (للتحقق)
    public decimal Quantity     { get; set; }
    public decimal UnitPrice    { get; set; }
    public decimal LineTotal    => Quantity * UnitPrice;
}
