namespace supermarket.Models;

/// <summary>نموذج الوردية — TASK-023</summary>
public class TreasuryShift
{
    public int      Id              { get; set; }
    public string   ShiftNumber     { get; set; } = string.Empty;
    public int      CashierId       { get; set; }
    public string   CashierName     { get; set; } = string.Empty;
    public int?     SafeId          { get; set; }
    public string   SafeName        { get; set; } = string.Empty;
    public decimal  OpeningBalance  { get; set; }
    public decimal  ExpectedClosing { get; set; }
    public decimal? ActualClosing   { get; set; }
    public decimal? Difference      { get; set; }
    public DateTime OpenedAt        { get; set; }
    public DateTime? ClosedAt       { get; set; }
    public string   Status          { get; set; } = "open"; // open | closed
    public string   Notes           { get; set; } = string.Empty;

    public string StatusAr => Status == "open" ? "🟢 مفتوحة" : "🔴 مغلقة";
    public bool   IsOpen   => Status == "open";
}

/// <summary>ملخص إحصائيات الوردية لتقرير الإغلاق</summary>
public class ShiftSummary
{
    public int     ShiftId         { get; set; }
    public decimal OpeningBalance  { get; set; }
    public decimal TotalSales      { get; set; }
    public decimal TotalReturns    { get; set; }
    public decimal TotalExpenses   { get; set; }
    public decimal ExpectedClosing => OpeningBalance + TotalSales - TotalReturns - TotalExpenses;
}
