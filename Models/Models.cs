namespace supermarket.Models;

// ── نموذج المستخدم ──────────────────────────────────────────
public class User
{
    public int      Id            { get; set; }
    public string   Username      { get; set; } = string.Empty;
    public string   PasswordHash  { get; set; } = string.Empty;
    public string   FullName      { get; set; } = string.Empty;
    public int      RoleId        { get; set; }
    public string   RoleName      { get; set; } = string.Empty;
    public string   RoleNameAr    { get; set; } = string.Empty;
    public bool     IsActive      { get; set; }
    public DateTime? LastLogin    { get; set; }
    public int      LoginAttempts { get; set; }
    public DateTime? LockedUntil  { get; set; }
    public DateTime  CreatedAt    { get; set; }
}

// ── نموذج الصنف ─────────────────────────────────────────────
public class Item
{
    public int      Id             { get; set; }
    public string   ItemCode       { get; set; } = string.Empty;
    public string   NameAr         { get; set; } = string.Empty;
    public string   NameEn         { get; set; } = string.Empty;
    public string   Barcode        { get; set; } = string.Empty;
    public string   BarcodeType    { get; set; } = "EAN-13";
    public int?     GroupId        { get; set; }
    public string   GroupName      { get; set; } = string.Empty;
    public int?     TypeId         { get; set; }
    public string   TypeName       { get; set; } = string.Empty;
    public int?     UnitId         { get; set; }
    public string   UnitName       { get; set; } = string.Empty;
    public int?     SupplierId     { get; set; }
    public string   SupplierName   { get; set; } = string.Empty;
    public decimal  PurchasePrice  { get; set; }
    public decimal  RetailPrice    { get; set; }
    public decimal  WholesalePrice { get; set; }
    public decimal  TaxRate        { get; set; }
    public decimal  ReorderPoint   { get; set; }
    public string?  ImagePath      { get; set; }
    public bool     IsActive       { get; set; } = true;
    public string   Notes          { get; set; } = string.Empty;
    public DateTime CreatedAt      { get; set; }
    public decimal  CurrentStock   { get; set; }  // من stock_levels
}

// ── نموذج المورد ─────────────────────────────────────────────
public class Supplier
{
    public int     Id          { get; set; }
    public string  Code        { get; set; } = string.Empty;
    public string  Name        { get; set; } = string.Empty;
    public string  Phone       { get; set; } = string.Empty;
    public string  Mobile      { get; set; } = string.Empty;
    public string  Email       { get; set; } = string.Empty;
    public string  Address     { get; set; } = string.Empty;
    public string  TaxNumber   { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public string  Notes       { get; set; } = string.Empty;
    public bool    IsActive    { get; set; } = true;
}

// ── نموذج العميل ─────────────────────────────────────────────
public class Customer
{
    public int     Id            { get; set; }
    public string  Code          { get; set; } = string.Empty;
    public string  Name          { get; set; } = string.Empty;
    public string  Phone         { get; set; } = string.Empty;
    public string  Email         { get; set; } = string.Empty;
    public string  Address       { get; set; } = string.Empty;
    public decimal CreditLimit   { get; set; }
    public decimal LoyaltyPoints { get; set; }
    public bool    IsActive      { get; set; } = true;
}

// ── مجموعة الأصناف ───────────────────────────────────────────
public class ItemGroup
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string NameAr    { get; set; } = string.Empty;
    public bool   IsActive  { get; set; } = true;
    public int    ItemCount { get; set; }   // عدد الأصناف المرتبطة
}

// ── نوع الصنف ────────────────────────────────────────────────
public class ItemType
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string NameAr    { get; set; } = string.Empty;
    public bool   IsActive  { get; set; } = true;
    public int    ItemCount { get; set; }   // عدد الأصناف المرتبطة
}

// ── وحدة القياس ──────────────────────────────────────────────
public class Unit
{
    public int    Id     { get; set; }
    public string Name   { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

// ── المستودع ─────────────────────────────────────────────────
public class Warehouse
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Location  { get; set; } = string.Empty;
    public bool   IsDefault { get; set; }
    public bool   IsActive  { get; set; } = true;
}

// ── سطر كشف حساب العميل ─────────────────────────────────────
public class CustomerStatement
{
    public string   InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate   { get; set; }
    public decimal  NetTotal      { get; set; }
    public decimal  PaidAmount    { get; set; }
    public decimal  Remaining     { get; set; }
    public string   PaymentMethod { get; set; } = string.Empty;
    public string   Status        { get; set; } = string.Empty;
    public string   Notes         { get; set; } = string.Empty;

    public string StatusAr => Status switch
    {
        "completed"  => "✅ مكتملة",
        "cancelled"  => "❌ ملغية",
        _            => Status
    };
    public string PaymentAr => PaymentMethod switch
    {
        "cash"   => "نقدي",
        "visa"   => "فيزا",
        "credit" => "آجل",
        _        => PaymentMethod
    };
}

// ── سطر كشف حساب المورد ─────────────────────────────────────
public class SupplierStatement
{
    public string   InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate   { get; set; }
    public decimal  NetTotal      { get; set; }
    public decimal  PaidAmount    { get; set; }
    public decimal  Remaining     { get; set; }
    public string   PaymentMethod { get; set; } = string.Empty;
    public string   Status        { get; set; } = string.Empty;
    public string   Notes         { get; set; } = string.Empty;

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
