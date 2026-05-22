using supermarket.Services;

namespace supermarket.Views;

internal sealed class DashboardView : UserControl
{
    public event Action<string, string>? ModuleRequested;

    // Colors
    private static readonly Color BgPage   = ColorTranslator.FromHtml("#F0F4F8");
    private static readonly Color BgCard   = Color.White;
    private static readonly Color TextDark = ColorTranslator.FromHtml("#1A2A3A");
    private static readonly Color TextMuted= ColorTranslator.FromHtml("#6B7A8D");
    private static readonly Color BannerTop= ColorTranslator.FromHtml("#1A3A5C");
    private static readonly Color BannerBot= ColorTranslator.FromHtml("#2563A8");

    // Responsive refs
    private TableLayoutPanel? _kpiTable;
    private TableLayoutPanel? _modulesGrid;
    private Panel?            _kpiSection;

    public DashboardView()
    {
        Dock        = DockStyle.Fill;
        BackColor   = BgPage;
        AutoScroll  = true;
        RightToLeft = RightToLeft.Yes;
        BuildUI();
        Resize += (_, _) => ApplyResponsiveLayout();
    }

    // ---------------------------------------------------------------
    //  Responsive Layout
    // ---------------------------------------------------------------
    private void ApplyResponsiveLayout()
    {
        int w = Width;

        // KPI columns: 2 on small (<600), 4 on medium+
        if (_kpiTable != null)
        {
            int kpiCols = w < 600 ? 2 : 4;
            if (_kpiTable.ColumnCount != kpiCols)
            {
                _kpiTable.ColumnCount = kpiCols;
                _kpiTable.ColumnStyles.Clear();
                for (int i = 0; i < kpiCols; i++)
                    _kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / kpiCols));
            }
        }

        // KPI section height
        if (_kpiSection != null)
        {
            int kpiCols2 = w < 600 ? 2 : 4;
            _kpiSection.Height = kpiCols2 == 2 ? 200 : 108;
        }

        // Modules columns: 1 (<480), 2 (<800), 3 (<1200), 4 (>=1200)
        if (_modulesGrid != null)
        {
            int cols = w < 480 ? 1 : w < 800 ? 2 : w < 1200 ? 3 : 4;
            if (_modulesGrid.ColumnCount != cols)
            {
                int totalCards = _modulesGrid.Controls.Count;
                var cards = new List<Control>();
                foreach (Control c in _modulesGrid.Controls)
                    cards.Add(c);

                _modulesGrid.Controls.Clear();
                _modulesGrid.ColumnCount = cols;
                _modulesGrid.ColumnStyles.Clear();
                for (int i = 0; i < cols; i++)
                    _modulesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));

                int rows = (int)Math.Ceiling((double)totalCards / cols);
                _modulesGrid.RowCount = rows;
                _modulesGrid.RowStyles.Clear();
                for (int i = 0; i < rows; i++)
                    _modulesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 138f));

                for (int i = 0; i < cards.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    cards[i].Dock = DockStyle.Fill;
                    _modulesGrid.Controls.Add(cards[i], col, row);
                }
            }
        }
    }

    // ---------------------------------------------------------------
    //  BUILD UI
    // ---------------------------------------------------------------
    private void BuildUI()
    {
        var banner        = BuildBanner();
        var kpiSection    = BuildKpiSection();
        var sectionHdr    = BuildSectionHeader("\u26a1  \u0627\u0644\u0648\u0635\u0648\u0644 \u0627\u0644\u0633\u0631\u064a\u0639 \u0644\u0644\u0648\u062d\u062f\u0627\u062a");
        var modulesScroll = BuildModulesPanel();

        Controls.Add(modulesScroll);
        Controls.Add(sectionHdr);
        Controls.Add(kpiSection);
        Controls.Add(banner);
    }

    // ---------------------------------------------------------------
    //  BANNER
    // ---------------------------------------------------------------
    private Panel BuildBanner()
    {
        var banner = new Panel { Dock = DockStyle.Top, Height = 100, RightToLeft = RightToLeft.Yes };
        banner.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                banner.ClientRectangle, BannerTop, BannerBot,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(brush, banner.ClientRectangle);
            using var pen = new Pen(Color.FromArgb(60, 120, 200), 2);
            g.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
        };

        var iconLbl = new Label
        {
            Text = "\U0001f3e0", Font = new Font("Segoe UI Emoji", 28F),
            ForeColor = Color.FromArgb(100, 180, 255), Dock = DockStyle.Left,
            Width = 80, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent
        };

        var infoPanel = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            Padding = new Padding(20, 0, 20, 0), RightToLeft = RightToLeft.Yes
        };
        infoPanel.Controls.Add(new Label
        {
            Text = $"\u0627\u0644\u064a\u0648\u0645: {GetArabicDate()}", Font = new Font("Tahoma", 9F),
            ForeColor = Color.FromArgb(150, 200, 255), Dock = DockStyle.Bottom,
            Height = 28, TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent
        });
        infoPanel.Controls.Add(new Label
        {
            Text = $"\u0645\u0631\u062d\u0628\u0627\u064b\u060c {SessionContext.DisplayName} \U0001f44b",
            Font = new Font("Tahoma", 17F, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent
        });

        var subtitleLbl = new Label
        {
            Text = "\U0001f4cc \u0644\u0648\u062d\u0629 \u0627\u0644\u062a\u062d\u0643\u0645 \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629",
            Font = new Font("Tahoma", 9F), ForeColor = Color.FromArgb(150, 200, 255),
            Dock = DockStyle.Right, Width = 200, TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        banner.Controls.Add(infoPanel);
        banner.Controls.Add(subtitleLbl);
        banner.Controls.Add(iconLbl);
        return banner;
    }

    // ---------------------------------------------------------------
    //  KPI SECTION
    // ---------------------------------------------------------------
    private Panel BuildKpiSection()
    {
        _kpiSection = new Panel
        {
            Dock = DockStyle.Top, Height = 108,
            BackColor = BgPage, Padding = new Padding(20, 10, 20, 8),
            RightToLeft = RightToLeft.Yes
        };

        _kpiTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
            BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            RightToLeft = RightToLeft.Yes
        };
        _kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++)
            _kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        (string Icon, string Value, string Label, string Hex)[] kpis =
        {
            ("\U0001f4b0", "\u0660 \u062c.\u0645",  "\u0645\u0628\u064a\u0639\u0627\u062a \u0627\u0644\u064a\u0648\u0645",  "#2563A8"),
            ("\U0001f9fe", "\u0660",      "\u0639\u062f\u062f \u0627\u0644\u0641\u0648\u0627\u062a\u064a\u0631",   "#16A34A"),
            ("\U0001f4e6", "\u0660",      "\u0623\u0635\u0646\u0627\u0641 \u0645\u0646\u062e\u0641\u0636\u0629",   "#D97706"),
            ("\U0001f465", "\u0660",      "\u0639\u0645\u0644\u0627\u0621 \u062c\u062f\u062f",      "#7C3AED"),
        };

        for (int i = 0; i < kpis.Length; i++)
        {
            var (ico, val, lbl, hex) = kpis[i];
            var card = BuildKpiCard(ico, val, lbl, hex);
            card.Dock   = DockStyle.Fill;
            card.Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 0);
            _kpiTable.Controls.Add(card, i, 0);
        }

        _kpiSection.Controls.Add(_kpiTable);
        return _kpiSection;
    }

    private static Panel BuildKpiCard(string icon, string value, string label, string hex)
    {
        var accent = ColorTranslator.FromHtml(hex);
        var card   = new Panel { BackColor = BgCard, Cursor = Cursors.Default };
        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var tb  = new SolidBrush(accent);
            g.FillRectangle(tb, 0, 0, card.Width, 4);
            using var pen = new Pen(Color.FromArgb(220, 228, 240));
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var iconLbl = new Label
        {
            Text = icon, Font = new Font("Segoe UI Emoji", 22F),
            ForeColor = accent, Dock = DockStyle.Left, Width = 54,
            TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent
        };

        var info = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 10, 4), RightToLeft = RightToLeft.Yes
        };
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

    // ---------------------------------------------------------------
    //  SECTION HEADER
    // ---------------------------------------------------------------
    private static Panel BuildSectionHeader(string text)
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top, Height = 44, BackColor = BgPage,
            Padding = new Padding(24, 0, 24, 0), RightToLeft = RightToLeft.Yes
        };
        bar.Controls.Add(new Label
        {
            Text = text, Font = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = TextDark, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent
        });
        return bar;
    }

    // ---------------------------------------------------------------
    //  MODULES PANEL
    // ---------------------------------------------------------------
    private Panel BuildModulesPanel()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill, BackColor = BgPage, AutoScroll = true,
            Padding = new Padding(20, 6, 20, 24), RightToLeft = RightToLeft.Yes
        };

        _modulesGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top, ColumnCount = 3, RowCount = 3,
            BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            RightToLeft = RightToLeft.Yes
        };
        for (int i = 0; i < 3; i++)
            _modulesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        for (int i = 0; i < 3; i++)
            _modulesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 138F));

        (string Icon, string Title, string Desc, string Hex)[] modules =
        {
            ("\U0001f6d2", "\u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a",   "\u0646\u0642\u0637\u0629 \u0627\u0644\u0628\u064a\u0639 \u00b7 \u0627\u0644\u0639\u0645\u0644\u0627\u0621\n\u0627\u0644\u0639\u0631\u0648\u0636 \u00b7 \u0627\u0644\u0645\u0631\u062a\u062c\u0639\u0627\u062a",  "#2563A8"),
            ("\U0001f4e6", "\u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a",  "\u0641\u0648\u0627\u062a\u064a\u0631 \u0627\u0644\u0634\u0631\u0627\u0621 \u00b7 \u0627\u0644\u0645\u0648\u0631\u062f\u064a\u0646\n\u0645\u0631\u062a\u062c\u0639\u0627\u062a \u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a", "#16A34A"),
            ("\U0001f3ea", "\u0627\u0644\u0623\u0635\u0646\u0627\u0641",    "\u0628\u0637\u0627\u0642\u0629 \u0627\u0644\u0635\u0646\u0641 \u00b7 \u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f\n\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629",  "#7C3AED"),
            ("\U0001f4ca", "\u0627\u0644\u0645\u062e\u0632\u0648\u0646",    "\u0627\u0644\u0623\u0631\u0635\u062f\u0629 \u00b7 \u0627\u0644\u062c\u0631\u062f\n\u0627\u0644\u062a\u062d\u0648\u064a\u0644\u0627\u062a \u00b7 \u0627\u0644\u062a\u0646\u0628\u064a\u0647\u0627\u062a",   "#D97706"),
            ("\U0001f4b0", "\u0627\u0644\u062e\u0632\u064a\u0646\u0629",    "\u0627\u0644\u0648\u0631\u062f\u064a\u0627\u062a\n\u0633\u0646\u062f\u0627\u062a \u0627\u0644\u0642\u0628\u0636 \u0648\u0627\u0644\u0635\u0631\u0641",  "#DC2626"),
            ("\U0001f3f7\ufe0f", "\u0627\u0644\u062a\u0633\u0639\u064a\u0631",   "\u062a\u062d\u062f\u064a\u062b \u0627\u0644\u0623\u0633\u0639\u0627\u0631 \u0627\u0644\u062c\u0645\u0627\u0639\u064a\n\u0633\u062c\u0644 \u062a\u063a\u064a\u064a\u0631\u0627\u062a \u0627\u0644\u0623\u0633\u0639\u0627\u0631",  "#0D9488"),
            ("\U0001f4f7", "\u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f",   "\u0637\u0628\u0627\u0639\u0629 \u0627\u0644\u0645\u0644\u0635\u0642\u0627\u062a\n\u062a\u0648\u0644\u064a\u062f QR Code",  "#1D4ED8"),
            ("\U0001f4c8", "\u0627\u0644\u062a\u0642\u0627\u0631\u064a\u0631",   "\u0645\u0628\u064a\u0639\u0627\u062a \u00b7 \u0645\u0634\u062a\u0631\u064a\u0627\u062a\n\u0645\u062e\u0632\u0648\u0646 \u00b7 \u0631\u0628\u062d\u064a\u0629",  "#92400E"),
            ("\u2699\ufe0f", "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a", "\u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0623\u0633\u0627\u0633\u064a\u0629\n\u0627\u0644\u0645\u0633\u062a\u0648\u062f\u0639\u0627\u062a \u00b7 \u0627\u0644\u0645\u0633\u062a\u062e\u062f\u0645\u0648\u0646",  "#374151"),
        };

        for (int i = 0; i < modules.Length; i++)
        {
            var (ico, ttl, dsc, hex) = modules[i];
            int col = i % 3;
            int row = i / 3;
            var card = BuildModuleCard(ico, ttl, dsc, hex);
            card.Dock   = DockStyle.Fill;
            card.Margin = new Padding(col < 2 ? 0 : 0, 0, col > 0 ? 12 : 0, 12);
            _modulesGrid.Controls.Add(card, col, row);
        }

        scroll.Controls.Add(_modulesGrid);
        return scroll;
    }

    // ---------------------------------------------------------------
    //  MODULE CARD
    // ---------------------------------------------------------------
    private Panel BuildModuleCard(string icon, string title, string desc, string hex)
    {
        var accent = ColorTranslator.FromHtml(hex);
        var card   = new Panel { BackColor = BgCard, Cursor = Cursors.Hand, RightToLeft = RightToLeft.Yes };
        bool hovered = false;

        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (hovered)
                g.FillRectangle(new SolidBrush(Color.FromArgb(12, accent.R, accent.G, accent.B)), card.ClientRectangle);
            using var tb  = new SolidBrush(accent);
            g.FillRectangle(tb, 0, 0, card.Width, 5);
            using var pen = new Pen(hovered ? Color.FromArgb(180, accent.R, accent.G, accent.B) : Color.FromArgb(215, 225, 238));
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var iconLbl = new Label
        {
            Text = icon, Font = new Font("Segoe UI Emoji", 26F), ForeColor = accent,
            Dock = DockStyle.Right, Width = 64, TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent, Padding = new Padding(0, 5, 0, 0)
        };

        var textPanel = new Panel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent,
            Padding = new Padding(10, 8, 6, 6), RightToLeft = RightToLeft.Yes
        };

        var openLbl = new Label
        {
            Text = "\u0641\u062a\u062d \u2190", Font = new Font("Tahoma", 8.5F, FontStyle.Bold),
            ForeColor = accent, Dock = DockStyle.Bottom, Height = 22,
            TextAlign = ContentAlignment.BottomRight, Cursor = Cursors.Hand, BackColor = Color.Transparent
        };
        var descLbl = new Label
        {
            Text = desc, Font = new Font("Tahoma", 8.5F), ForeColor = TextMuted,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopRight, BackColor = Color.Transparent
        };
        var titleLbl = new Label
        {
            Text = title, Font = new Font("Tahoma", 12F, FontStyle.Bold), ForeColor = TextDark,
            Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.TopRight, BackColor = Color.Transparent
        };

        textPanel.Controls.Add(descLbl);
        textPanel.Controls.Add(openLbl);
        textPanel.Controls.Add(titleLbl);
        card.Controls.Add(textPanel);
        card.Controls.Add(iconLbl);

        void SetHover(bool on) { hovered = on; card.Invalidate(); }
        foreach (Control c in new Control[] { card, iconLbl, textPanel, titleLbl, descLbl, openLbl })
        {
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) => SetHover(false);
            c.Click      += (_, _) => ModuleRequested?.Invoke(title, desc);
        }
        return card;
    }

    // ---------------------------------------------------------------
    //  HELPERS
    // ---------------------------------------------------------------
    private static string GetArabicDate()
    {
        var now = DateTime.Now;
        string[] days   = { "\u0627\u0644\u0623\u062d\u062f","\u0627\u0644\u0627\u062b\u0646\u064a\u0646","\u0627\u0644\u062b\u0644\u0627\u062b\u0627\u0621","\u0627\u0644\u0623\u0631\u0628\u0639\u0627\u0621","\u0627\u0644\u062e\u0645\u064a\u0633","\u0627\u0644\u062c\u0645\u0639\u0629","\u0627\u0644\u0633\u0628\u062a" };
        string[] months = { "\u064a\u0646\u0627\u064a\u0631","\u0641\u0628\u0631\u0627\u064a\u0631","\u0645\u0627\u0631\u0633","\u0623\u0628\u0631\u064a\u0644","\u0645\u0627\u064a\u0648","\u064a\u0648\u0646\u064a\u0648","\u064a\u0648\u0644\u064a\u0648","\u0623\u063a\u0633\u0637\u0633","\u0633\u0628\u062a\u0645\u0628\u0631","\u0623\u0643\u062a\u0648\u0628\u0631","\u0646\u0648\u0641\u0645\u0628\u0631","\u062f\u064a\u0633\u0645\u0628\u0631" };
        return $"{days[(int)now.DayOfWeek]}\u060c {now.Day} {months[now.Month - 1]} {now.Year}";
    }
}
