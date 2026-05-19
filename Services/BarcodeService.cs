using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace supermarket.Services;

/// <summary>توليد صور الباركود بـ ZXing.Net — TASK-025</summary>
internal static class BarcodeService
{
    // ══ توليد صورة الباركود ══════════════════════════════════

    /// <summary>يولّد Bitmap لأي نوع باركود</summary>
    public static Bitmap GenerateBarcode(string content, string barcodeType,
                                          int width = 250, int height = 80)
    {
        if (string.IsNullOrWhiteSpace(content))
            return CreateEmptyBitmap(width, height);

        try
        {
            var writer = new BarcodeWriter<Bitmap>
            {
                Format  = ParseFormat(barcodeType),
                Options = new EncodingOptions
                {
                    Width  = width,
                    Height = height,
                    Margin = 4,
                    PureBarcode = false
                },
                Renderer = new BitmapRenderer()
            };

            // QR له خيارات مختلفة
            if (barcodeType.Equals("QR", StringComparison.OrdinalIgnoreCase) ||
                barcodeType.Equals("QR_CODE", StringComparison.OrdinalIgnoreCase))
            {
                writer.Options = new QrCodeEncodingOptions
                {
                    Width = width, Height = height, Margin = 2
                };
            }

            return writer.Write(content);
        }
        catch
        {
            return CreateEmptyBitmap(width, height, "خطأ في الباركود");
        }
    }

    // ══ رسم ملصق كامل ════════════════════════════════════════

    /// <summary>يرسم ملصق كامل (شركة + اسم + باركود + سعر) على Bitmap</summary>
    public static Bitmap DrawLabel(Models.BarcodeLabel lbl, int width, int height,
                                    bool showCompany, bool showPrice, bool showName,
                                    string companyName = "Smart Market")
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        int y = 2;

        // اسم الشركة
        if (showCompany)
        {
            using var fComp = new Font("Arial", 7f, FontStyle.Bold);
            var sz = g.MeasureString(companyName, fComp);
            g.DrawString(companyName, fComp, Brushes.DarkBlue,
                (width - sz.Width) / 2f, y);
            y += (int)sz.Height + 1;
        }

        // اسم الصنف
        if (showName && !string.IsNullOrEmpty(lbl.ItemName))
        {
            using var fName = new Font("Arial", 6.5f);
            var name = lbl.ItemName.Length > 28
                ? lbl.ItemName[..25] + "..."
                : lbl.ItemName;
            var sz = g.MeasureString(name, fName);
            g.DrawString(name, fName, Brushes.Black,
                (width - sz.Width) / 2f, y);
            y += (int)sz.Height + 1;
        }

        // صورة الباركود
        int barcodeH = showPrice ? height - y - 18 : height - y - 4;
        barcodeH = Math.Max(barcodeH, 30);

        if (!string.IsNullOrEmpty(lbl.Barcode))
        {
            using var barImg = GenerateBarcode(lbl.Barcode, lbl.BarcodeType,
                                                width - 8, barcodeH);
            g.DrawImage(barImg, 4, y, width - 8, barcodeH);
        }
        y += barcodeH + 1;

        // السعر
        if (showPrice)
        {
            using var fPrice = new Font("Arial", 7.5f, FontStyle.Bold);
            var priceStr = "السعر: " + lbl.PriceDisplay;
            var sz = g.MeasureString(priceStr, fPrice);
            g.DrawString(priceStr, fPrice, Brushes.DarkGreen,
                (width - sz.Width) / 2f, y);
        }

        // إطار
        using var pen = new Pen(Color.LightGray, 1);
        g.DrawRectangle(pen, 0, 0, width - 1, height - 1);

        return bmp;
    }

    // ══ التحقق من صحة EAN-13 ═════════════════════════════════

    public static bool IsValidEan13(string barcode)
    {
        if (barcode.Length != 13 || !barcode.All(char.IsDigit)) return false;
        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (barcode[i] - '0') * (i % 2 == 0 ? 1 : 3);
        int check = (10 - (sum % 10)) % 10;
        return check == (barcode[12] - '0');
    }

    /// <summary>يولّد رقم EAN-13 عشوائياً صحيح checksum</summary>
    public static string GenerateEan13()
    {
        var rnd = new Random();
        var digits = Enumerable.Range(0, 12)
                               .Select(_ => rnd.Next(0, 10))
                               .ToArray();
        int sum = digits.Select((d, i) => d * (i % 2 == 0 ? 1 : 3)).Sum();
        int check = (10 - (sum % 10)) % 10;
        return string.Concat(digits) + check;
    }

    // ── helpers ───────────────────────────────────────────────

    private static BarcodeFormat ParseFormat(string t) => t.ToUpper() switch
    {
        "EAN-13" or "EAN13"  => BarcodeFormat.EAN_13,
        "CODE-128" or "CODE128" => BarcodeFormat.CODE_128,
        "QR" or "QR_CODE"    => BarcodeFormat.QR_CODE,
        "EAN-8"  or "EAN8"   => BarcodeFormat.EAN_8,
        "UPC-A"  or "UPCA"   => BarcodeFormat.UPC_A,
        _                    => BarcodeFormat.CODE_128
    };

    private static Bitmap CreateEmptyBitmap(int w, int h, string? msg = null)
    {
        var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.WhiteSmoke);
        if (msg != null)
        {
            using var f = new Font("Arial", 8f);
            g.DrawString(msg, f, Brushes.Gray, 4, h / 2 - 8);
        }
        return bmp;
    }
}
