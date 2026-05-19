namespace supermarket.Models;

// ── ملصق باركود لصنف واحد ────────────────────────────────────
public class BarcodeLabel
{
    public int     ItemId       { get; set; }
    public string  ItemCode     { get; set; } = string.Empty;
    public string  ItemName     { get; set; } = string.Empty;
    public string  Barcode      { get; set; } = string.Empty;
    public string  BarcodeType  { get; set; } = "EAN-13";
    public decimal RetailPrice  { get; set; }
    public string  GroupName    { get; set; } = string.Empty;
    public int     Copies       { get; set; } = 1;
    public string  PriceDisplay => RetailPrice.ToString("N2") + " ج.م";
}

// ── طلب طباعة جماعية ─────────────────────────────────────────
public class LabelPrintJob
{
    public List<BarcodeLabel> Labels      { get; set; } = new();
    public int    LabelsPerRow  { get; set; } = 3;
    public int    LabelWidth    { get; set; } = 180;
    public int    LabelHeight   { get; set; } = 100;
    public bool   ShowPrice     { get; set; } = true;
    public bool   ShowItemName  { get; set; } = true;
    public bool   ShowCompany   { get; set; } = true;
    public string CompanyName   { get; set; } = "Smart Market";
    public int    TotalCopies   => Labels.Sum(l => l.Copies);
}
