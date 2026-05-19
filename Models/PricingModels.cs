namespace supermarket.Models;

// ── سجل تاريخ الأسعار ────────────────────────────────────────
public class PriceHistoryEntry
{
    public int      Id          { get; set; }
    public int      ItemId      { get; set; }
    public string   ItemCode    { get; set; } = string.Empty;
    public string   ItemName    { get; set; } = string.Empty;
    public string   PriceType   { get; set; } = string.Empty;
    public decimal  OldPrice    { get; set; }
    public decimal  NewPrice    { get; set; }
    public decimal  Change      => NewPrice - OldPrice;
    public decimal  ChangePct   => OldPrice == 0 ? 0
                                    : Math.Round((Change / OldPrice) * 100, 2);
    public string   ChangedBy   { get; set; } = string.Empty;
    public DateTime ChangedAt   { get; set; }
    public string   Reason      { get; set; } = string.Empty;

    public string PriceTypeAr => PriceType switch
    {
        "retail"     => "تجزئة",
        "wholesale"  => "جملة",
        "purchase"   => "شراء",
        "promo"      => "ترويجي",
        _            => PriceType
    };
}

// ── معاينة صف تحديث جماعي ────────────────────────────────────
public class BulkPricePreviewRow
{
    public int     ItemId       { get; set; }
    public string  ItemCode     { get; set; } = string.Empty;
    public string  ItemName     { get; set; } = string.Empty;
    public string  GroupName    { get; set; } = string.Empty;
    public string  SupplierName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal NewPrice     { get; set; }
    public decimal Change       => NewPrice - CurrentPrice;
    public decimal ChangePct    => CurrentPrice == 0 ? 0
                                    : Math.Round((Change / CurrentPrice) * 100, 2);
    public bool    Selected     { get; set; } = true;
}

// ── طلب التحديث الجماعي ──────────────────────────────────────
public class BulkPriceUpdateRequest
{
    /// retail | wholesale | promo
    public string  PriceType    { get; set; } = "retail";

    /// percent_up | percent_down | amount_up | amount_down | fixed
    public string  Method       { get; set; } = "percent_up";
    public decimal Value        { get; set; }

    /// null = الكل
    public int?    GroupId      { get; set; }
    public int?    SupplierId   { get; set; }

    public string  Reason       { get; set; } = string.Empty;

    /// الصفوف المحددة للتطبيق (بعد المعاينة)
    public List<int> ItemIds    { get; set; } = new();
}
