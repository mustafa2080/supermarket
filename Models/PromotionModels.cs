namespace supermarket.Models;

// ── نموذج العرض ──────────────────────────────────────────────
public class Promotion
{
    public int      Id            { get; set; }
    public string   Name          { get; set; } = string.Empty;
    public string   Type          { get; set; } = "percentage"; // percentage | buy_x_get_y | bogo
    public decimal? DiscountValue { get; set; }   // نسبة الخصم % أو قيمة ثابتة
    public int?     BuyQuantity   { get; set; }   // اشتري X
    public int?     GetQuantity   { get; set; }   // احصل على Y
    public decimal? GetPrice      { get; set; }   // بسعر (اختياري)
    public string   AppliesTo     { get; set; } = "item"; // item | group
    public int?     ItemId        { get; set; }
    public string   ItemName      { get; set; } = string.Empty;
    public int?     GroupId       { get; set; }
    public string   GroupName     { get; set; } = string.Empty;
    public DateTime StartDate     { get; set; } = DateTime.Today;
    public DateTime EndDate       { get; set; } = DateTime.Today.AddMonths(1);
    public bool     IsActive      { get; set; } = true;

    public bool IsValid => IsActive && DateTime.Today >= StartDate && DateTime.Today <= EndDate;

    public string TypeAr => Type switch
    {
        "percentage"   => "خصم نسبي %",
        "buy_x_get_y"  => "اشتري X احصل على Y",
        "bogo"         => "اشتري 1 واحصل على 1",
        _              => Type
    };

    public string AppliesToAr => AppliesTo switch
    {
        "item"  => "صنف محدد",
        "group" => "مجموعة أصناف",
        _       => AppliesTo
    };

    public string Summary => Type switch
    {
        "percentage"  => $"خصم {DiscountValue:N1}% على {(AppliesTo == "item" ? ItemName : GroupName)}",
        "buy_x_get_y" => $"اشتري {BuyQuantity} احصل على {GetQuantity}" +
                         (GetPrice.HasValue ? $" بسعر {GetPrice:N2}" : " مجاناً"),
        "bogo"        => $"اشتري {BuyQuantity} احصل على {GetQuantity} مجاناً",
        _             => Name
    };
}

// ── نتيجة تطبيق العرض على سطر ───────────────────────────────
public class PromotionResult
{
    public int     PromotionId   { get; set; }
    public string  PromotionName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }   // مبلغ الخصم على السطر
    public string  Description   { get; set; } = string.Empty;
}
