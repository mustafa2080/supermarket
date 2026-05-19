namespace supermarket.Models;

// ── فاتورة البيع ─────────────────────────────────────────────
public class SalesInvoice
{
    public int       Id             { get; set; }
    public string    InvoiceNumber  { get; set; } = string.Empty;
    public int?      CustomerId     { get; set; }
    public string    CustomerName   { get; set; } = string.Empty;
    public int       WarehouseId    { get; set; }
    public string    WarehouseName  { get; set; } = string.Empty;
    public int       CashierId      { get; set; }
    public int?      ShiftId        { get; set; }
    public DateTime  InvoiceDate    { get; set; } = DateTime.Now;
    public string    PaymentMethod  { get; set; } = "cash"; // cash | visa | credit
    public decimal   Subtotal       { get; set; }
    public decimal   Discount       { get; set; }
    public decimal   TaxAmount      { get; set; }
    public decimal   NetTotal       { get; set; }
    public decimal   PaidAmount     { get; set; }
    public decimal   ChangeAmount   { get; set; }
    public decimal   LoyaltyRedeemed { get; set; }
    public string    Status         { get; set; } = "completed";
    public string    Notes          { get; set; } = string.Empty;
    public List<SalesInvoiceLine> Lines { get; set; } = new();

    public string PaymentAr => PaymentMethod switch
    {
        "cash"   => "نقدي",
        "visa"   => "فيزا",
        "credit" => "آجل",
        _        => PaymentMethod
    };
}

// ── سطر فاتورة البيع ─────────────────────────────────────────
public class SalesInvoiceLine
{
    public int     Id          { get; set; }
    public int     InvoiceId   { get; set; }
    public int     ItemId      { get; set; }
    public string  ItemCode    { get; set; } = string.Empty;
    public string  ItemName    { get; set; } = string.Empty;
    public string  UnitName    { get; set; } = string.Empty;
    public decimal Quantity    { get; set; } = 1;
    public decimal UnitPrice   { get; set; }
    public decimal Discount    { get; set; }
    public int?    PromotionId   { get; set; }
    public string  PromotionName { get; set; } = string.Empty;
    public decimal TaxRate     { get; set; }
    public decimal TaxAmount   { get; set; }
    public decimal LineTotal   { get; set; }
    public decimal StockQty    { get; set; } // للتحقق من الرصيد
}

// ── مرتجع مبيعات ─────────────────────────────────────────────
public class SalesReturn
{
    public int      Id                 { get; set; }
    public string   ReturnNumber       { get; set; } = string.Empty;
    public int?     OriginalInvoiceId  { get; set; }
    public string   OriginalInvoiceNum { get; set; } = string.Empty;
    public string   CustomerName       { get; set; } = string.Empty;
    public int      CashierId          { get; set; }
    public string   CashierName        { get; set; } = string.Empty;
    public int      WarehouseId        { get; set; }
    public DateTime ReturnDate         { get; set; } = DateTime.Now;
    public decimal  TotalAmount        { get; set; }
    public string   RefundMethod       { get; set; } = "cash"; // cash | credit_note
    public string   Status             { get; set; } = "completed";
    public string   Notes              { get; set; } = string.Empty;
    public List<SalesReturnLine> Lines { get; set; } = new();

    public string RefundMethodAr => RefundMethod == "cash" ? "نقدي" : "رصيد دائن";
}

// ── سطر مرتجع مبيعات ─────────────────────────────────────────
public class SalesReturnLine
{
    public int     Id           { get; set; }
    public int     ReturnId     { get; set; }
    public int     ItemId       { get; set; }
    public string  ItemCode     { get; set; } = string.Empty;
    public string  ItemName     { get; set; } = string.Empty;
    public string  UnitName     { get; set; } = string.Empty;
    public decimal SoldQty      { get; set; } // الكمية المباعة الأصلية
    public decimal ReturnQty    { get; set; } // الكمية المرتجعة (يُدخلها المستخدم)
    public decimal UnitPrice    { get; set; }
    public decimal LineTotal    => ReturnQty * UnitPrice;
}

// ── فاتورة معلقة (Hold) ──────────────────────────────────────
public class HoldInvoice
{
    public string              HoldRef     { get; set; } = string.Empty; // مرجع داخلي
    public string              CustomerRef { get; set; } = string.Empty; // اسم/ملاحظة اختيارية
    public List<SalesInvoiceLine> Lines    { get; set; } = new();
    public decimal             Subtotal    { get; set; }
    public int?                CustomerId  { get; set; }
    public string              CustomerName { get; set; } = string.Empty;
    public DateTime            HeldAt      { get; set; } = DateTime.Now;
}
