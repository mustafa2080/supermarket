using supermarket.Services;
using supermarket.Theme;
using supermarket.Views;

namespace supermarket;

public sealed class MainForm : Form
{
    private Panel  _contentHost  = null!;
    private Label  _pageTitle    = null!;
    private Label  _clockLabel   = null!;
    private Label  _dateLabel    = null!;
    private Panel? _activeSideBtn;
    private readonly System.Windows.Forms.Timer _clock;

    // Responsive sidebar refs
    private Panel?           _sidebar    = null;
    private FlowLayoutPanel? _navFlow    = null;
    private Panel?           _logoPanel  = null;
    private bool             _collapsed  = false;

    private static readonly Color SidebarBg     = ColorTranslator.FromHtml("#0D2137");
    private static readonly Color SidebarActive = ColorTranslator.FromHtml("#1B4F72");
    private static readonly Color SidebarHover  = ColorTranslator.FromHtml("#163354");
    private static readonly Color SidebarText   = Color.FromArgb(210, 230, 255);
    private static readonly Color SidebarMuted  = Color.FromArgb(100, 140, 180);
    private static readonly Color AccentGold    = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color HeaderBg      = ColorTranslator.FromHtml("#1B4F72");
    private static readonly Color ContentBg     = ColorTranslator.FromHtml("#EEF2F7");

    private record NavItem(string Icon, string Title, string Desc);
    private static readonly NavItem[] Nav =
    {
        new("\U0001f3e0", "\u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629",   "\u0644\u0648\u062d\u0629 \u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0648\u0627\u0644\u0625\u062d\u0635\u0627\u0626\u064a\u0627\u062a"),
        new("\U0001f6d2", "\u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a",   "\u0646\u0642\u0637\u0629 \u0627\u0644\u0628\u064a\u0639 \u0648\u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0648\u0627\u0644\u0639\u0631\u0648\u0636 \u0648\u0627\u0644\u0645\u0631\u062a\u062c\u0639\u0627\u062a"),
        new("\U0001f4e6", "\u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a",  "\u0627\u0644\u0645\u0648\u0631\u062f\u064a\u0646 \u0648\u0641\u0648\u0627\u062a\u064a\u0631 \u0627\u0644\u0634\u0631\u0627\u0621 \u0648\u0627\u0644\u0645\u0631\u062a\u062c\u0639\u0627\u062a"),
        new("\U0001f3ea", "\u0627\u0644\u0623\u0635\u0646\u0627\u0641",    "\u0628\u0637\u0627\u0642\u0629 \u0627\u0644\u0635\u0646\u0641 \u0648\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629"),
        new("\U0001f4ca", "\u0627\u0644\u0645\u062e\u0632\u0648\u0646",    "\u0627\u0644\u0623\u0631\u0635\u062f\u0629 \u0648\u0627\u0644\u062c\u0631\u062f \u0648\u0627\u0644\u062a\u062d\u0648\u064a\u0644\u0627\u062a \u0648\u0627\u0644\u062a\u0646\u0628\u064a\u0647\u0627\u062a"),
        new("\U0001f4b0", "\u0627\u0644\u062e\u0632\u064a\u0646\u0629",    "\u0627\u0644\u0648\u0631\u062f\u064a\u0627\u062a \u0648\u0633\u0646\u062f\u0627\u062a \u0627\u0644\u0642\u0628\u0636 \u0648\u0627\u0644\u0635\u0631\u0641"),
        new("\U0001f3f7\ufe0f", "\u0627\u0644\u062a\u0633\u0639\u064a\u0631",    "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0623\u0633\u0639\u0627\u0631 \u0648\u062a\u062d\u062f\u064a\u062b\u0647\u0627 \u0627\u0644\u062c\u0645\u0627\u0639\u064a"),
        new("\U0001f4f7", "\u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f",   "\u0637\u0628\u0627\u0639\u0629 \u0645\u0644\u0635\u0642\u0627\u062a \u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f \u0648\u062a\u0648\u0644\u064a\u062f QR"),
        new("\U0001f4c8", "\u0627\u0644\u062a\u0642\u0627\u0631\u064a\u0631",   "\u062a\u0642\u0627\u0631\u064a\u0631 \u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a \u0648\u0627\u0644\u0645\u062e\u0632\u0648\u0646 \u0648\u0627\u0644\u0631\u0628\u062d\u064a\u0629"),
        new("\u2699\ufe0f", "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a",  "\u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0623\u0633\u0627\u0633\u064a\u0629 \u0648\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a \u0627\u0644\u0639\u0627\u0645\u0629"),
    };

    public MainForm()
    {
        Text              = "Smart Market ERP";
        StartPosition     = FormStartPosition.CenterScreen;
        MinimumSize       = new Size(800, 600);
        WindowState       = FormWindowState.Maximized;
        BackColor         = SidebarBg;
        Font              = AppTheme.BodyFont;
        RightToLeft       = RightToLeft.No;
        RightToLeftLayout = false;

        BuildLayout();

        _clock = new System.Windows.Forms.Timer { Interval = 1000 };
        _clock.Tick += (_, _) => TickClock();
        _clock.Start();
        TickClock();

        NavigateTo(Nav[0]);

        // Responsive: adjust sidebar on resize
        Resize += (_, _) => ApplyResponsiveSidebar();
    }

    // ---------------------------------------------------------------
    //  Responsive Sidebar
    // ---------------------------------------------------------------
    private void ApplyResponsiveSidebar()
    {
        if (_sidebar == null || _navFlow == null) return;

        int formW = Width;

        // Breakpoints:
        // < 900  => collapsed (icon-only, 60px)
        // 900-1200 => narrow (160px)
        // > 1200 => full (240px)
        int targetW;
        bool collapse;
        if (formW < 900)
        {
            targetW  = 60;
            collapse = true;
        }
        else if (formW < 1200)
        {
            targetW  = 180;
            collapse = false;
        }
        else
        {
            targetW  = 240;
            collapse = false;
        }

        if (_sidebar.Width == targetW && _collapsed == collapse) return;
        _sidebar.Width = targetW;
        _collapsed     = collapse;

        // Update logo panel visibility
        if (_logoPanel != null)
            _logoPanel.Height = collapse ? 60 : 100;

        // Update each nav button
        foreach (Control c in _navFlow.Controls)
        {
            if (c is not Panel btn) continue;
            btn.Width = targetW - 4;

            foreach (Control child in btn.Controls)
            {
                if (child is Label lbl)
                {
                    // title label (Fill dock) => hide text when collapsed
                    if (child.Dock == DockStyle.Fill)
                        lbl.Visible = !collapse;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    //  BUILD LAYOUT
    // ---------------------------------------------------------------
    private void BuildLayout()
    {
        _sidebar      = BuildSidebar();
        _sidebar.Dock  = DockStyle.Right;
        _sidebar.Width = 240;

        var mainArea = BuildMainArea();
        mainArea.Dock = DockStyle.Fill;

        Controls.Add(mainArea);
        Controls.Add(_sidebar);
    }

    // ---------------------------------------------------------------
    //  SIDEBAR
    // ---------------------------------------------------------------
    private Panel BuildSidebar()
    {
        var sidebar = new Panel { BackColor = SidebarBg, Width = 240 };

        var gold = new Panel { Dock = DockStyle.Right, Width = 4, BackColor = AccentGold };

        _logoPanel = new Panel
        {
            Dock      = DockStyle.Top, Height = 100,
            BackColor = ColorTranslator.FromHtml("#091829"),
            Padding   = new Padding(0, 14, 0, 10)
        };
        _logoPanel.Controls.Add(new Label
        {
            Text = "\U0001f6d2", Font = new Font("Segoe UI Emoji", 22F), ForeColor = Color.White,
            Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.MiddleCenter
        });
        _logoPanel.Controls.Add(new Label
        {
            Text = "Smart Market ERP", Font = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Top, Height = 24,
            TextAlign = ContentAlignment.MiddleCenter
        });
        _logoPanel.Controls.Add(new Label
        {
            Text = "\u0646\u0638\u0627\u0645 \u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0633\u0648\u0628\u0631 \u0645\u0627\u0631\u0643\u062a",
            Font = new Font("Tahoma", 8F), ForeColor = SidebarMuted,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopCenter
        });

        // User info panel
        var userPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 64,
            BackColor = ColorTranslator.FromHtml("#0F2840"),
            Padding = new Padding(14, 10, 14, 10)
        };
        var userAvatar = new Label
        {
            Text = "\U0001f464", Font = new Font("Segoe UI Emoji", 16F), ForeColor = Color.White,
            Dock = DockStyle.Right, Width = 36, TextAlign = ContentAlignment.MiddleCenter
        };
        var userInfo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        userInfo.Controls.Add(new Label
        {
            Text = SessionContext.RoleAr, Font = new Font("Tahoma", 8F),
            ForeColor = Color.FromArgb(140, 185, 230), Dock = DockStyle.Bottom,
            Height = 18, TextAlign = ContentAlignment.MiddleRight
        });
        userInfo.Controls.Add(new Label
        {
            Text = SessionContext.DisplayName, Font = new Font("Tahoma", 10F, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight
        });
        userPanel.Controls.Add(userInfo);
        userPanel.Controls.Add(userAvatar);

        var navSectionLabel = new Label
        {
            Text = "  \u0627\u0644\u0642\u0627\u0626\u0645\u0629 \u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629",
            Font = new Font("Tahoma", 7.5F, FontStyle.Bold), ForeColor = SidebarMuted,
            Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 14, 0), BackColor = SidebarBg
        };

        var navScroll = new Panel { Dock = DockStyle.Fill, BackColor = SidebarBg, AutoScroll = false };
        _navFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = SidebarBg,
            Padding = new Padding(0, 4, 0, 4), AutoScroll = false
        };
        foreach (var item in Nav)
            _navFlow.Controls.Add(CreateNavButton(item));
        navScroll.Controls.Add(_navFlow);

        var logoutWrap = new Panel
        {
            Dock = DockStyle.Bottom, Height = 52,
            BackColor = ColorTranslator.FromHtml("#091829"),
            Padding = new Padding(12, 10, 12, 10)
        };
        var logoutBtn = new Button
        {
            Text = "\U0001f513   \u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c",
            Dock = DockStyle.Fill, Font = new Font("Tahoma", 9.5F),
            ForeColor = Color.FromArgb(255, 120, 120), BackColor = Color.FromArgb(50, 20, 20),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter
        };
        logoutBtn.FlatAppearance.BorderColor = Color.FromArgb(100, 50, 50);
        logoutBtn.FlatAppearance.BorderSize  = 1;
        logoutBtn.MouseEnter += (_, _) => logoutBtn.BackColor = Color.FromArgb(90, 30, 30);
        logoutBtn.MouseLeave += (_, _) => logoutBtn.BackColor = Color.FromArgb(50, 20, 20);
        logoutBtn.Click      += (_, _) => Logout();
        logoutWrap.Controls.Add(logoutBtn);

        sidebar.Controls.Add(navScroll);
        sidebar.Controls.Add(navSectionLabel);
        sidebar.Controls.Add(userPanel);
        sidebar.Controls.Add(_logoPanel);
        sidebar.Controls.Add(logoutWrap);
        sidebar.Controls.Add(gold);
        return sidebar;
    }

    private Panel CreateNavButton(NavItem item)
    {
        var btn = new Panel
        {
            Width = 236, Height = 44, BackColor = SidebarBg,
            Cursor = Cursors.Hand, Tag = item, Margin = Padding.Empty
        };
        var activeBar = new Panel { Dock = DockStyle.Right, Width = 3, BackColor = Color.Transparent };
        var icon = new Label
        {
            Text = item.Icon, Font = new Font("Segoe UI Emoji", 13F), ForeColor = SidebarText,
            Dock = DockStyle.Right, Width = 42, TextAlign = ContentAlignment.MiddleCenter
        };
        var title = new Label
        {
            Text = item.Title, Font = new Font("Tahoma", 10F), ForeColor = SidebarText,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };
        btn.Controls.Add(title);
        btn.Controls.Add(icon);
        btn.Controls.Add(activeBar);

        void Hover(bool on) { if (btn == _activeSideBtn) return; btn.BackColor = on ? SidebarHover : SidebarBg; }
        foreach (Control c in new Control[] { btn, title, icon })
        {
            c.MouseEnter += (_, _) => Hover(true);
            c.MouseLeave += (_, _) => Hover(false);
            c.Click      += (_, _) => NavigateTo(item);
        }
        return btn;
    }

    private void SetActiveNav(NavItem item)
    {
        if (_activeSideBtn != null)
        {
            _activeSideBtn.BackColor = SidebarBg;
            foreach (Control c in _activeSideBtn.Controls)
                if (c.Dock == DockStyle.Right && c.Width == 3) c.BackColor = Color.Transparent;
        }
        Panel? found = null;
        foreach (Control ctrl in Controls)
        {
            found = FindNavPanel(ctrl, item);
            if (found != null) break;
        }
        if (found == null) return;
        found.BackColor = SidebarActive;
        foreach (Control c in found.Controls)
        {
            if (c.Dock == DockStyle.Right && c.Width == 3) c.BackColor = AccentGold;
            if (c is Label lbl) lbl.ForeColor = Color.White;
        }
        _activeSideBtn = found;
    }

    private static Panel? FindNavPanel(Control root, NavItem item)
    {
        if (root is Panel p && p.Tag is NavItem n && n == item) return p;
        foreach (Control c in root.Controls)
        {
            var r = FindNavPanel(c, item);
            if (r != null) return r;
        }
        return null;
    }

    // ---------------------------------------------------------------
    //  MAIN AREA
    // ---------------------------------------------------------------
    private Control BuildMainArea()
    {
        var area = new Panel { BackColor = ContentBg };
        var header = BuildHeader();
        header.Dock   = DockStyle.Top;
        header.Height = 68;
        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(190, 205, 220) };
        var contentWrap = new Panel
        {
            Dock = DockStyle.Fill, BackColor = ContentBg, Padding = new Padding(18, 14, 18, 14)
        };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        card.Paint += (s, e) =>
        {
            using var p = new Pen(Color.FromArgb(200, 215, 232), 1);
            e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
        };
        _contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0) };
        card.Controls.Add(_contentHost);
        contentWrap.Controls.Add(card);
        area.Controls.Add(contentWrap);
        area.Controls.Add(sep);
        area.Controls.Add(header);
        return area;
    }

    private Panel BuildHeader()
    {
        var header = new Panel { BackColor = HeaderBg, Padding = new Padding(20, 0, 20, 0) };
        var clockWrap = new Panel
        {
            Dock = DockStyle.Left, Width = 220, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 8)
        };
        _clockLabel = new Label
        {
            Text = "", Font = new Font("Consolas", 20F, FontStyle.Bold),
            ForeColor = Color.White, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleLeft
        };
        _dateLabel = new Label
        {
            Text = "", Font = new Font("Tahoma", 8.5F),
            ForeColor = Color.FromArgb(170, 210, 255), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft
        };
        clockWrap.Controls.Add(_dateLabel);
        clockWrap.Controls.Add(_clockLabel);

        _pageTitle = new Label
        {
            Text = "", Font = new Font("Tahoma", 15F, FontStyle.Bold), ForeColor = Color.White,
            Dock = DockStyle.Right, Width = 340, TextAlign = ContentAlignment.MiddleRight
        };
        var centerLbl = new Label
        {
            Text = "Smart Market ERP", Font = new Font("Tahoma", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 200, 250), Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        header.Controls.Add(centerLbl);
        header.Controls.Add(_pageTitle);
        header.Controls.Add(clockWrap);
        return header;
    }

    private void TickClock()
    {
        var now = DateTime.Now;
        _clockLabel.Text = now.ToString("HH:mm:ss");
        string[] days = { "\u0627\u0644\u0623\u062d\u062f","\u0627\u0644\u0627\u062b\u0646\u064a\u0646","\u0627\u0644\u062b\u0644\u0627\u062b\u0627\u0621","\u0627\u0644\u0623\u0631\u0628\u0639\u0627\u0621","\u0627\u0644\u062e\u0645\u064a\u0633","\u0627\u0644\u062c\u0645\u0639\u0629","\u0627\u0644\u0633\u0628\u062a" };
        _dateLabel.Text  = $"{days[(int)now.DayOfWeek]}  {now:dd/MM/yyyy}";
    }

    // ---------------------------------------------------------------
    //  NAVIGATION
    // ---------------------------------------------------------------
    private void NavigateTo(NavItem item)
    {
        _pageTitle.Text = $"{item.Icon}  {item.Title}";
        SetActiveNav(item);
        SetContent(ResolveView(item.Title));
    }

    private Control ResolveView(string title) => title switch
    {
        "\u0627\u0644\u0631\u0626\u064a\u0633\u064a\u0629"  => BuildDashboard(),
        "\u0627\u0644\u0645\u0628\u064a\u0639\u0627\u062a"  => BuildTabs(
            ("\U0001f6d2 \u0646\u0642\u0637\u0629 \u0627\u0644\u0628\u064a\u0639",       new PosView()),
            ("\U0001f465 \u0627\u0644\u0639\u0645\u0644\u0627\u0621",          new CustomersView()),
            ("\U0001f381 \u0627\u0644\u0639\u0631\u0648\u0636 \u0648\u0627\u0644\u062e\u0635\u0648\u0645\u0627\u062a", new PromotionsView()),
            ("\U0001f504 \u0627\u0644\u0645\u0631\u062a\u062c\u0639\u0627\u062a",        new SalesReturnsView())),
        "\u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a" => BuildTabs(
            ("\U0001f4c4 \u0641\u0648\u0627\u062a\u064a\u0631 \u0627\u0644\u0634\u0631\u0627\u0621", new PurchaseInvoicesView()),
            ("\U0001f504 \u0627\u0644\u0645\u0631\u062a\u062c\u0639\u0627\u062a",     new PurchaseReturnsView()),
            ("\U0001f3ed \u0627\u0644\u0645\u0648\u0631\u062f\u064a\u0646",      new SuppliersView())),
        "\u0627\u0644\u0623\u0635\u0646\u0627\u0641"   => new ItemsView    { Dock = DockStyle.Fill },
        "\u0627\u0644\u0645\u062e\u0632\u0648\u0646"   => BuildTabs(
            ("\U0001f4ca \u0645\u0633\u062a\u0648\u0649 \u0627\u0644\u0645\u062e\u0632\u0648\u0646", new StockLevelView()),
            ("\U0001f4e6 \u0627\u0644\u062c\u0631\u062f",         new InventoryCountView()),
            ("\U0001f504 \u0627\u0644\u062a\u062d\u0648\u064a\u0644\u0627\u062a",     new TransferView()),
            ("\U0001f5d1\ufe0f \u0627\u0644\u062a\u0627\u0644\u0641",        new DamageView()),
            ("\U0001f3ed \u0627\u0644\u0645\u0633\u062a\u0648\u062f\u0639\u0627\u062a",    EmbedForm<WarehousesManagerForm>())),
        "\u0627\u0644\u062e\u0632\u064a\u0646\u0629"   => new TreasuryView  { Dock = DockStyle.Fill },
        "\u0627\u0644\u062a\u0633\u0639\u064a\u0631"   => new PricingView   { Dock = DockStyle.Fill },
        "\u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f"  => new BarcodeView   { Dock = DockStyle.Fill },
        "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a" => new CategoriesView{ Dock = DockStyle.Fill },
        _           => Placeholder(title)
    };

    private static TabControl BuildTabs(params (string T, Control V)[] pages)
    {
        var tc = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
        foreach (var (t, v) in pages)
        {
            var page = new TabPage(t) { BackColor = AppTheme.Surface };
            v.Dock = DockStyle.Fill;
            page.Controls.Add(v);
            tc.TabPages.Add(page);
        }
        return tc;
    }

    private static Control EmbedForm<T>() where T : Form, new()
    {
        var f = new T { Dock = DockStyle.Fill, TopLevel = false, FormBorderStyle = FormBorderStyle.None };
        return f;
    }

    private static Control Placeholder(string title)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        p.Controls.Add(new Label
        {
            Text = $"\U0001f527  {title}\n\n\u0647\u0630\u0647 \u0627\u0644\u0648\u062d\u062f\u0629 \u0642\u064a\u062f \u0627\u0644\u062a\u0637\u0648\u064a\u0631",
            Font = new Font("Tahoma", 13F), ForeColor = AppTheme.MutedText,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter
        });
        return p;
    }

    private Control BuildDashboard()
    {
        var dash = new DashboardView { Dock = DockStyle.Fill };
        dash.ModuleRequested += (t, _) =>
        {
            var item = Array.Find(Nav, n => n.Title == t);
            if (item != null) NavigateTo(item);
        };
        return dash;
    }

    private void SetContent(Control c)
    {
        _contentHost.Controls.Clear();
        c.Dock = DockStyle.Fill;
        _contentHost.Controls.Add(c);
        if (c is Form f && !f.Visible) f.Show();
    }

    // ---------------------------------------------------------------
    //  LOGOUT
    // ---------------------------------------------------------------
    private void Logout()
    {
        if (MessageBox.Show("\u0647\u0644 \u062a\u0631\u064a\u062f \u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c\u061f", "\u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u062e\u0631\u0648\u062c",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        if (SessionContext.CurrentUser != null)
            AuditService.LogLogout(SessionContext.CurrentUser.Id);
        SessionContext.EndSession();
        Hide();
        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK)
        {
            Show();
            NavigateTo(Nav[0]);
        }
        else Application.Exit();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clock.Stop();
        _clock.Dispose();
        base.OnFormClosed(e);
    }
}
