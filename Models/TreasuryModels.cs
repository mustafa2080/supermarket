namespace supermarket.Models;

// ── الخزينة ──────────────────────────────────────────────────
public class Safe
{
    public int     Id        { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  NameAr    { get; set; } = string.Empty;
    public decimal Balance   { get; set; }
    public bool    IsDefault { get; set; }
    public bool    IsActive  { get; set; } = true;
}

// ── بند مصروف ────────────────────────────────────────────────
public class ExpenseItem
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string NameAr    { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}

// ── مصدر إيراد ───────────────────────────────────────────────
public class RevenueSource
{
    public int    Id     { get; set; }
    public string Name   { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
}

// ── سند صرف ──────────────────────────────────────────────────
public class PaymentVoucher
{
    public int      Id              { get; set; }
    public string   VoucherNumber   { get; set; } = string.Empty;
    public int      SafeId          { get; set; }
    public string   SafeName        { get; set; } = string.Empty;
    public decimal  Amount          { get; set; }
    public int?     ExpenseItemId   { get; set; }
    public string   ExpenseItemName { get; set; } = string.Empty;
    public string   Description     { get; set; } = string.Empty;
    public string   ReferenceType   { get; set; } = "manual"; // purchase_invoice | manual
    public int?     ReferenceId     { get; set; }
    public DateTime VoucherDate     { get; set; } = DateTime.Today;
    public string   CreatedBy       { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }

    public string TypeAr => ReferenceType == "purchase_invoice" ? "فاتورة شراء" : "يدوي";
}

// ── سند قبض ──────────────────────────────────────────────────
public class ReceiptVoucher
{
    public int      Id                { get; set; }
    public string   VoucherNumber     { get; set; } = string.Empty;
    public int      SafeId            { get; set; }
    public string   SafeName          { get; set; } = string.Empty;
    public decimal  Amount            { get; set; }
    public int?     RevenueSourceId   { get; set; }
    public string   RevenueSourceName { get; set; } = string.Empty;
    public string   Description       { get; set; } = string.Empty;
    public string   ReferenceType     { get; set; } = "manual"; // sales_invoice | manual
    public int?     ReferenceId       { get; set; }
    public DateTime VoucherDate       { get; set; } = DateTime.Today;
    public string   CreatedBy         { get; set; } = string.Empty;
    public DateTime CreatedAt         { get; set; }

    public string TypeAr => ReferenceType == "sales_invoice" ? "فاتورة بيع" : "يدوي";
}
