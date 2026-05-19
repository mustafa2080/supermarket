namespace supermarket.Models;

// ── فاتورة الشراء ────────────────────────────────────────────
public class PurchaseInvoice
{
    public int      Id            { get; set; }
    public string   InvoiceNumber { get; set; } = string.Empty;
    public int      SupplierId    { get; set; }
    public string   SupplierName  { get; set; } = string.Empty;
    public int      WarehouseId   { get; set; }
    public string   WarehouseName { get; set; } = string.Empty;
    public DateTime InvoiceDate   { get; set; } = DateTime.Today;
    public string   PaymentMethod { get; set; } = "cash";  // cash | credit | check
    public decimal  Subtotal      { get; set; }
    public decimal  Discount      { get; set; }
    public decimal  TaxAmount     { get; set; }
    public decimal  NetTotal      { get; set; }
    public decimal  PaidAmount    { get; set; }
    public decimal  Remaining     { get; set; }
    public string   Status        { get; set; } = "draft"; // draft | approved | cancelled
    public string   Notes         { get; set; } = string.Empty;
    public int?     CreatedBy     { get; set; }
    public int?     ApprovedBy    { get; set; }
    public DateTime? ApprovedAt   { get; set; }
    public DateTime CreatedAt     { get; set; }
    public List<PurchaseInvoiceLine> Lines { get; set; } = new();

    public string StatusAr => Status switch
    {
        "approved"  => "✅ معتمدة",
        "draft"     => "📝 مسودة",
        "cancelled" => "❌ ملغية",
        _           => Status
    };

    public string PaymentAr => PaymentMethod switch
    {
        "cash"   => "نقدي",
        "credit" => "آجل",
        "check"  => "شيك",
        _        => PaymentMethod
    };
}

// ── سطر فاتورة الشراء ────────────────────────────────────────
public class PurchaseInvoiceLine
{
    public int     Id         { get; set; }
    public int     InvoiceId  { get; set; }
    public int     ItemId     { get; set; }
    public string  ItemCode   { get; set; } = string.Empty;
    public string  ItemName   { get; set; } = string.Empty;
    public string  UnitName   { get; set; } = string.Empty;
    public decimal Quantity   { get; set; }
    public decimal UnitPrice  { get; set; }
    public decimal Discount   { get; set; }   // مبلغ الخصم
    public decimal TaxRate    { get; set; }   // نسبة الضريبة %
    public decimal TaxAmount  { get; set; }
    public decimal LineTotal  { get; set; }
    public string  Notes      { get; set; } = string.Empty;

    // حساب تلقائي
    public decimal Subtotal   => (Quantity * UnitPrice) - Discount;
    public void RecalcTax()   => TaxAmount = Subtotal * TaxRate / 100;
    public void RecalcTotal() { RecalcTax(); LineTotal = Subtotal + TaxAmount; }
}
