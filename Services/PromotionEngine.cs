using supermarket.Models;

namespace supermarket.Services;

/// <summary>
/// TASK-016 — محرك العروض والخصومات.
/// يُطبَّق على سطر واحد من سلة POS مع مراعاة أولوية العروض.
/// </summary>
internal static class PromotionEngine
{
    /// <summary>
    /// يجلب أفضل عرض واحد لصنف محدد بناءً على:
    ///  - النوع: percentage → buy_x_get_y → bogo
    ///  - الأولوية: أعلى خصم ينتصر
    /// </summary>
    public static PromotionResult? Apply(
        Item item,
        decimal quantity,
        List<Promotion> activePromotions)
    {
        // جمع كل العروض المنطبقة على هذا الصنف
        var candidates = activePromotions
            .Where(p => Matches(p, item))
            .ToList();

        if (candidates.Count == 0) return null;

        PromotionResult? best = null;

        foreach (var p in candidates)
        {
            var result = Calculate(p, item, quantity);
            if (result is null) continue;

            // اختيار الأعلى خصماً
            if (best is null || result.DiscountAmount > best.DiscountAmount)
                best = result;
        }

        return best;
    }

    // ── هل العرض ينطبق على هذا الصنف؟ ──────────────────────
    private static bool Matches(Promotion p, Item item)
    {
        return p.AppliesTo switch
        {
            "item"  => p.ItemId  == item.Id,
            "group" => p.GroupId == item.GroupId,
            _       => false
        };
    }

    // ── حساب الخصم بناءً على نوع العرض ──────────────────────
    private static PromotionResult? Calculate(Promotion p, Item item, decimal qty)
    {
        decimal discount = 0;
        string  desc     = string.Empty;

        switch (p.Type)
        {
            // ── خصم نسبي ─────────────────────────────────────
            case "percentage":
                if (!p.DiscountValue.HasValue || p.DiscountValue <= 0) return null;
                discount = qty * item.RetailPrice * (p.DiscountValue.Value / 100m);
                desc = $"خصم {p.DiscountValue:N1}%";
                break;

            // ── اشتري X احصل على Y بسعر أقل/مجاناً ──────────
            case "buy_x_get_y":
                if (!p.BuyQuantity.HasValue || !p.GetQuantity.HasValue) return null;
                int buyQ   = p.BuyQuantity.Value;
                int getQ   = p.GetQuantity.Value;
                int cycles = (int)(qty / (buyQ + getQ));
                if (cycles <= 0) break;

                decimal freeUnits = cycles * getQ;
                decimal priceEach = p.GetPrice.HasValue && p.GetPrice.Value < item.RetailPrice
                    ? item.RetailPrice - p.GetPrice.Value   // فرق السعر
                    : item.RetailPrice;                      // مجاناً
                discount = freeUnits * priceEach;
                desc = p.GetPrice.HasValue
                    ? $"اشتري {buyQ} احصل على {getQ} بسعر {p.GetPrice:N2}"
                    : $"اشتري {buyQ} احصل على {getQ} مجاناً";
                break;

            // ── BOGO (اشتري واحد واحصل على واحد) ─────────────
            case "bogo":
                int buyB  = p.BuyQuantity ?? 1;
                int getB  = p.GetQuantity ?? 1;
                int cyclesB = (int)(qty / (buyB + getB));
                if (cyclesB <= 0) break;

                discount = cyclesB * getB * item.RetailPrice;
                desc = $"اشتري {buyB} واحصل على {getB} مجاناً";
                break;

            default:
                return null;
        }

        if (discount <= 0) return null;

        return new PromotionResult
        {
            PromotionId    = p.Id,
            PromotionName  = p.Name,
            DiscountAmount = Math.Round(discount, 2),
            Description    = desc
        };
    }
}
