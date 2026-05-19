using supermarket.Services;

namespace supermarket.Views;

internal sealed class DashboardView : UserControl
{
    public event Action<string, string>? ModuleRequested;

    // ── ألوان النظام ──────────────────────────────────────────────────
    private static readonly Color BgPage      = ColorTranslator.FromHtml("#F0F4F8");
    private static readonly Color BgCard      = Color.White;
    private static readonly Color TextDark    = ColorTranslator.FromHtml("#1A2A3A");
    private static readonly Color TextMuted   = ColorTranslator.FromHtml("#6B7A8D");
    private static readonly Color BannerTop   = ColorTranslator.FromHtml("#1A3A5C");
    private static readonly Color BannerBot   = ColorTranslator.FromHtml("#2563A8");

    public DashboardView()
    {
        Dock       = DockStyle.Fill;
        BackColor  = BgPage;
        AutoScroll = true;
        RightToLeft = RightToLeft.Yes;
        BuildUI();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BUILD UI
    // ═══════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        var banner     = BuildBanner();
        var kpiSection = BuildKpiSection();
        var sectionHdr = BuildSectionHeader("⚡  الوصول السريع للوحدات");
        var modulesScroll = BuildModulesPanel();

        Controls.Add(modulesScroll);   // Fill — أولاً
        Controls.Add(sectionHdr);      // Top
        Controls.Add(kpiSection);      // Top
        Controls.Add(banner);          // Top — آخراً ليظهر في الأعلى
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BANNER
    // ═══════════════════════════════════════════════════════════════════
    private Panel BuildBanner()
    {
        var banner = new Panel
        {
            Dock       = DockStyle.Top,
            Height     = 100,
            RightToLeft = RightToLeft.Yes
        };

        banner.Paint += (_, e) =>
        {
            var g  = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                banner.ClientRectangle, BannerTop, BannerBot,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(brush, banner.ClientRectangle);

            // خط مضيء في الأسفل
            using var pen = new Pen(Color.FromArgb(60, 120, 200), 2);
            g.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
        };

        // أيقونة المنزل (يسار)
        var iconLbl = new Label
        {
            Text      = "🏠",
            Font      = new Font("Segoe UI Emoji", 28F),
            ForeColor = Color.FromArgb(100, 180, 255),
            Dock      = DockStyle.Left,
            Width     = 80,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        // معلومات (يمين)
        var infoPanel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(20, 0, 20, 0),
            RightToLeft = RightToLeft.Yes
        };

        var dateLabel = new Label
        {
            Text      = $"اليوم: {GetArabicDate()}",
            Font      = new Font("Tahoma", 9F),
            ForeColor = Color.FromArgb(150, 200, 255),
            Dock      = DockStyle.Bottom,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };

        var welcomeLabel = new Label
        {
            Text      = $"مرحباً، {SessionContext.DisplayName} 👋",
            Font      = new Font("Tahoma", 17F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };

        infoPanel.Controls.Add(welcomeLabel);
        infoPanel.Controls.Add(dateLabel);

        // نص لوحة التحكم (يسار بعد الأيقونة)
        var subtitleLbl = new Label
        {
            Text      = "📌 لوحة التحكم الرئيسية",
            Font      = new Font("Tahoma", 9F),
            ForeColor = Color.FromArgb(150, 200, 255),
            Dock      = DockStyle.Right,
            Width     = 200,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        banner.Controls.Add(infoPanel);
        banner.Controls.Add(subtitleLbl);
        banner.Controls.Add(iconLbl);
        return banner;
    }


    // ═══════════════════════════════════════════════════════════════════
    //  KPI SECTION
    // ═══════════════════════════════════════════════════════════════════
    private Panel BuildKpiSection()
    {
        var section = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 108,
            BackColor = BgPage,
            Padding   = new Padding(20, 10, 20, 8),
            RightToLeft = RightToLeft.Yes
        };

        var tbl = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 4,
            RowCount        = 1,
            BackColor       = Color.Transparent,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            RightToLeft     = RightToLeft.Yes
        };
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        (string Icon, string Value, string Label, string Hex)[] kpis =
        {
            ("💰", "٠ ج.م",  "مبيعات اليوم",  "#2563A8"),
            ("🧾", "٠",      "عدد الفواتير",   "#16A34A"),
            ("📦", "٠",      "أصناف منخفضة",   "#D97706"),
            ("👥", "٠",      "عملاء جدد",      "#7C3AED"),
        };

        for (int i = 0; i < kpis.Length; i++)
        {
            var (ico, val, lbl, hex) = kpis[i];
            var card = BuildKpiCard(ico, val, lbl, hex);
            card.Dock   = DockStyle.Fill;
            card.Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 0);
            tbl.Controls.Add(card, i, 0);
        }

        section.Controls.Add(tbl);
        return section;
    }

    private static Panel BuildKpiCard(string icon, string value, string label, string hex)
    {
        var accent = ColorTranslator.FromHtml(hex);
        var light  = Color.FromArgb(20,
            accent.R < 235 ? accent.R + 20 : accent.R,
            accent.G < 235 ? accent.G + 20 : accent.G,
            accent.B < 235 ? accent.B + 20 : accent.B);

        var card = new Panel { BackColor = BgCard, Cursor = Cursors.Default };
        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // شريط علوي ملون
            using var tb = new SolidBrush(accent);
            g.FillRectangle(tb, 0, 0, card.Width, 4);
            // حدود خفيفة
            using var pen = new Pen(Color.FromArgb(220, 228, 240));
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // أيقونة
        var iconLbl = new Label
        {
            Text      = icon,
            Font      = new Font("Segoe UI Emoji", 22F),
            ForeColor = accent,
            Dock      = DockStyle.Left,
            Width     = 54,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent,
                               Padding = new Padding(0, 6, 10, 4), RightToLeft = RightToLeft.Yes };
        info.Controls.Add(new Label
        {
            Text = label, Font = new Font("Tahoma", 8F), ForeColor = TextMuted,
            Dock = DockStyle.Bottom, Height = 20, TextAlign = ContentAlignment.BottomRight,
            BackColor = Color.Transparent
        });
        info.Controls.Add(new Label
        {
            Text = value, Font = new Font("Tahoma", 15F, FontStyle.Bold), ForeColor = TextDark,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        });

        card.Controls.Add(info);
        card.Controls.Add(iconLbl);
        return card;
    }


    // ═══════════════════════════════════════════════════════════════════
    //  SECTION HEADER
    // ═══════════════════════════════════════════════════════════════════
    private static Panel BuildSectionHeader(string text)
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = BgPage,
            Padding   = new Padding(24, 0, 24, 0),
            RightToLeft = RightToLeft.Yes
        };
        bar.Controls.Add(new Label
        {
            Text      = text,
            Font      = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = TextDark,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        });
        return bar;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MODULES PANEL
    // ═══════════════════════════════════════════════════════════════════
    private Panel BuildModulesPanel()
    {
        var scroll = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = BgPage,
            AutoScroll  = true,
            Padding     = new Padding(20, 6, 20, 24),
            RightToLeft = RightToLeft.Yes
        };

        // شبكة 3 × 3 ثابتة ومنتظمة
        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            ColumnCount = 3,
            RowCount    = 3,
            BackColor   = Color.Transparent,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            AutoSize    = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            RightToLeft = RightToLeft.Yes
        };

        for (int i = 0; i < 3; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        for (int i = 0; i < 3; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 138F));

        (string Icon, string Title, string Desc, string Hex)[] modules =
        {
            ("🛒", "المبيعات",   "نقطة البيع · العملاء\nالعروض · المرتجعات",         "#2563A8"),
            ("📦", "المشتريات",  "فواتير الشراء · الموردين\nمرتجعات المشتريات",       "#16A34A"),
            ("🏪", "الأصناف",    "بطاقة الصنف · الباركود\nإدارة البيانات الرئيسية",   "#7C3AED"),
            ("📊", "المخزون",    "الأرصدة · الجرد\nالتحويلات · التنبيهات",            "#D97706"),
            ("💰", "الخزينة",    "الورديات\nسندات القبض والصرف",                      "#DC2626"),
            ("🏷️", "التسعير",   "تحديث الأسعار الجماعي\nسجل تغييرات الأسعار",        "#0D9488"),
            ("📷", "الباركود",   "طباعة الملصقات\nتوليد QR Code",                    "#1D4ED8"),
            ("📈", "التقارير",   "مبيعات · مشتريات\nمخزون · ربحية",                  "#92400E"),
            ("⚙️", "الإعدادات", "البيانات الأساسية\nالمستودعات · المستخدمون",         "#374151"),
        };

        for (int i = 0; i < modules.Length; i++)
        {
            var (ico, ttl, dsc, hex) = modules[i];
            int col = i % 3;
            int row = i / 3;
            var card = BuildModuleCard(ico, ttl, dsc, hex);
            card.Dock   = DockStyle.Fill;
            // هامش: يمين دائماً 0، يسار 12 إلا آخر عمود
            card.Margin = new Padding(col < 2 ? 0 : 0, 0, col > 0 ? 12 : 0, 12);
            grid.Controls.Add(card, col, row);
        }

        scroll.Controls.Add(grid);
        return scroll;
    }


    // ═══════════════════════════════════════════════════════════════════
    //  MODULE CARD
    // ═══════════════════════════════════════════════════════════════════
    private Panel BuildModuleCard(string icon, string title, string desc, string hex)
    {
        var accent = ColorTranslator.FromHtml(hex);

        var card = new Panel
        {
            BackColor   = BgCard,
            Cursor      = Cursors.Hand,
            RightToLeft = RightToLeft.Yes
        };

        bool hovered = false;

        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // خلفية عند hover
            if (hovered)
                g.FillRectangle(new SolidBrush(Color.FromArgb(12,
                    accent.R, accent.G, accent.B)), card.ClientRectangle);

            // شريط ملون في الأعلى
            using var tb = new SolidBrush(accent);
            g.FillRectangle(tb, 0, 0, card.Width, 5);

            // ظل خفيف / حدود
            using var pen = new Pen(hovered
                ? Color.FromArgb(180, accent.R, accent.G, accent.B)
                : Color.FromArgb(215, 225, 238));
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        // أيقونة
        var iconLbl = new Label
        {
            Text      = icon,
            Font      = new Font("Segoe UI Emoji", 26F),
            ForeColor = accent,
            Dock      = DockStyle.Right,
            Width     = 64,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 5, 0, 0)
        };

        // محتوى النص
        var textPanel = new Panel
        {
            Dock        = DockStyle.Fill,
            BackColor   = Color.Transparent,
            Padding     = new Padding(10, 8, 6, 6),
            RightToLeft = RightToLeft.Yes
        };

        var openLbl = new Label
        {
            Text      = "فتح ←",
            Font      = new Font("Tahoma", 8.5F, FontStyle.Bold),
            ForeColor = accent,
            Dock      = DockStyle.Bottom,
            Height    = 22,
            TextAlign = ContentAlignment.BottomRight,
            Cursor    = Cursors.Hand,
            BackColor = Color.Transparent
        };

        var descLbl = new Label
        {
            Text      = desc,
            Font      = new Font("Tahoma", 8.5F),
            ForeColor = TextMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopRight,
            BackColor = Color.Transparent
        };

        var titleLbl = new Label
        {
            Text      = title,
            Font      = new Font("Tahoma", 12F, FontStyle.Bold),
            ForeColor = TextDark,
            Dock      = DockStyle.Top,
            Height    = 30,
            TextAlign = ContentAlignment.TopRight,
            BackColor = Color.Transparent
        };

        textPanel.Controls.Add(descLbl);
        textPanel.Controls.Add(openLbl);
        textPanel.Controls.Add(titleLbl);

        card.Controls.Add(textPanel);
        card.Controls.Add(iconLbl);

        // Hover
        void SetHover(bool on)
        {
            hovered = on;
            card.Invalidate();
        }

        foreach (Control c in new Control[] { card, iconLbl, textPanel, titleLbl, descLbl, openLbl })
        {
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) => SetHover(false);
            c.Click      += (_, _) => ModuleRequested?.Invoke(title, desc);
        }

        return card;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════
    private static string GetArabicDate()
    {
        var now = DateTime.Now;
        string[] days   = { "الأحد","الاثنين","الثلاثاء","الأربعاء","الخميس","الجمعة","السبت" };
        string[] months = { "يناير","فبراير","مارس","أبريل","مايو","يونيو",
                             "يوليو","أغسطس","سبتمبر","أكتوبر","نوفمبر","ديسمبر" };
        return $"{days[(int)now.DayOfWeek]}، {now.Day} {months[now.Month - 1]} {now.Year}";
    }
}
