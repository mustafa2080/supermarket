using supermarket.Services;
using supermarket.Theme;
using supermarket.Views;

namespace supermarket;

/// <summary>الشاشة الرئيسية — Sidebar يمين ثابت + Header + Content</summary>
public sealed class MainForm : Form
{
    // ── عناصر UI ──────────────────────────────────────────────
    private Panel  _contentHost  = null!;
    private Label  _pageTitle    = null!;
    private Label  _clockLabel   = null!;
    private Label  _dateLabel    = null!;
    private Panel? _activeSideBtn;
    private readonly System.Windows.Forms.Timer _clock;

    // ── ألوان ─────────────────────────────────────────────────
    private static readonly Color SidebarBg     = ColorTranslator.FromHtml("#0D2137");
    private static readonly Color SidebarActive = ColorTranslator.FromHtml("#1B4F72");
    private static readonly Color SidebarHover  = ColorTranslator.FromHtml("#163354");
    private static readonly Color SidebarText   = Color.FromArgb(210, 230, 255);
    private static readonly Color SidebarMuted  = Color.FromArgb(100, 140, 180);
    private static readonly Color AccentGold    = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color HeaderBg      = ColorTranslator.FromHtml("#1B4F72");
    private static readonly Color ContentBg     = ColorTranslator.FromHtml("#EEF2F7");

    // ── وحدات التنقل ──────────────────────────────────────────
    private record NavItem(string Icon, string Title, string Desc);
    private static readonly NavItem[] Nav =
    {
        new("🏠", "الرئيسية",   "لوحة المتابعة والإحصائيات"),
        new("🛒", "المبيعات",   "نقطة البيع والعملاء والعروض والمرتجعات"),
        new("📦", "المشتريات",  "الموردين وفواتير الشراء والمرتجعات"),
        new("🏪", "الأصناف",    "بطاقة الصنف وإدارة البيانات الرئيسية"),
        new("📊", "المخزون",    "الأرصدة والجرد والتحويلات والتنبيهات"),
        new("💰", "الخزينة",    "الورديات وسندات القبض والصرف"),
        new("🏷️", "التسعير",    "إدارة الأسعار وتحديثها الجماعي"),
        new("📷", "الباركود",   "طباعة ملصقات الباركود وتوليد QR"),
        new("📈", "التقارير",   "تقارير المبيعات والمخزون والربحية"),
        new("⚙️", "الإعدادات",  "البيانات الأساسية والإعدادات العامة"),
    };

    public MainForm()
    {
        Text              = "Smart Market ERP";
        StartPosition     = FormStartPosition.CenterScreen;
        MinimumSize       = new Size(1280, 720);
        WindowState       = FormWindowState.Maximized;
        BackColor         = SidebarBg;
        Font              = AppTheme.BodyFont;
        // لا نضع RTL على الـ Form — نتحكم يدوياً بكل عنصر
        RightToLeft       = RightToLeft.No;
        RightToLeftLayout = false;

        BuildLayout();

        _clock = new System.Windows.Forms.Timer { Interval = 1000 };
        _clock.Tick += (_, _) => TickClock();
        _clock.Start();
        TickClock();

        NavigateTo(Nav[0]);
    }

    // ════════════════════════════════════════════════════════
    //  بناء الهيكل الرئيسي — Dock بدل TableLayout عشان RTL
    // ════════════════════════════════════════════════════════
    private void BuildLayout()
    {
        // Sidebar ثابت على اليمين
        var sidebar = BuildSidebar();
        sidebar.Dock  = DockStyle.Right;
        sidebar.Width = 240;

        // المنطقة الرئيسية تملأ الباقي
        var mainArea = BuildMainArea();
        mainArea.Dock = DockStyle.Fill;

        // نضيف الـ Fill أولاً ثم الـ Right
        Controls.Add(mainArea);
        Controls.Add(sidebar);
    }

    // ════════════════════════════════════════════════════════
    //  SIDEBAR
    // ════════════════════════════════════════════════════════
    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            BackColor = SidebarBg,
            Width     = 240
        };

        // شريط ذهبي أقصى اليمين
        var gold = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 4,
            BackColor = AccentGold
        };

        // ── Logo ─────────────────────────────────────────────
        var logo = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 100,
            BackColor = ColorTranslator.FromHtml("#091829"),
            Padding   = new Padding(0, 14, 0, 10)
        };
        logo.Controls.Add(new Label
        {
            Text      = "🛒",
            Font      = new Font("Segoe UI Emoji", 22F),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 36,
            TextAlign = ContentAlignment.MiddleCenter
        });
        logo.Controls.Add(new Label
        {
            Text      = "Smart Market ERP",
            Font      = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 24,
            TextAlign = ContentAlignment.MiddleCenter
        });
        logo.Controls.Add(new Label
        {
            Text      = "نظام إدارة السوبر ماركت",
            Font      = new Font("Tahoma", 8F),
            ForeColor = SidebarMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopCenter
        });

        // ── User Info ─────────────────────────────────────────
        var userPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 64,
            BackColor = ColorTranslator.FromHtml("#0F2840"),
            Padding   = new Padding(14, 10, 14, 10)
        };

        var userAvatar = new Label
        {
            Text      = "👤",
            Font      = new Font("Segoe UI Emoji", 16F),
            ForeColor = Color.White,
            Dock      = DockStyle.Right,
            Width     = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var userInfo = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        userInfo.Controls.Add(new Label
        {
            Text      = SessionContext.RoleAr,
            Font      = new Font("Tahoma", 8F),
            ForeColor = Color.FromArgb(140, 185, 230),
            Dock      = DockStyle.Bottom,
            Height    = 18,
            TextAlign = ContentAlignment.MiddleRight
        });
        userInfo.Controls.Add(new Label
        {
            Text      = SessionContext.DisplayName,
            Font      = new Font("Tahoma", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        });
        userPanel.Controls.Add(userInfo);
        userPanel.Controls.Add(userAvatar);

        // ── Nav Label ─────────────────────────────────────────
        var navSectionLabel = new Label
        {
            Text      = "  القائمة الرئيسية",
            Font      = new Font("Tahoma", 7.5F, FontStyle.Bold),
            ForeColor = SidebarMuted,
            Dock      = DockStyle.Top,
            Height    = 30,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 14, 0),
            BackColor = SidebarBg
        };

        // ── Nav Buttons ───────────────────────────────────────
        var navScroll = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = SidebarBg,
            AutoScroll = false
        };

        var navFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            BackColor     = SidebarBg,
            Padding       = new Padding(0, 4, 0, 4),
            AutoScroll    = false
        };

        foreach (var item in Nav)
            navFlow.Controls.Add(CreateNavButton(item));

        navScroll.Controls.Add(navFlow);

        // ── Logout ────────────────────────────────────────────
        var logoutWrap = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 52,
            BackColor = ColorTranslator.FromHtml("#091829"),
            Padding   = new Padding(12, 10, 12, 10)
        };
        var logoutBtn = new Button
        {
            Text      = "🔓   تسجيل الخروج",
            Dock      = DockStyle.Fill,
            Font      = new Font("Tahoma", 9.5F),
            ForeColor = Color.FromArgb(255, 120, 120),
            BackColor = Color.FromArgb(50, 20, 20),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        logoutBtn.FlatAppearance.BorderColor = Color.FromArgb(100, 50, 50);
        logoutBtn.FlatAppearance.BorderSize  = 1;
        logoutBtn.MouseEnter += (_, _) => logoutBtn.BackColor = Color.FromArgb(90, 30, 30);
        logoutBtn.MouseLeave += (_, _) => logoutBtn.BackColor = Color.FromArgb(50, 20, 20);
        logoutBtn.Click      += (_, _) => Logout();
        logoutWrap.Controls.Add(logoutBtn);

        // ── تجميع Sidebar ─────────────────────────────────────
        sidebar.Controls.Add(navScroll);
        sidebar.Controls.Add(navSectionLabel);
        sidebar.Controls.Add(userPanel);
        sidebar.Controls.Add(logo);
        sidebar.Controls.Add(logoutWrap);
        sidebar.Controls.Add(gold);

        return sidebar;
    }

    private Panel CreateNavButton(NavItem item)
    {
        var btn = new Panel
        {
            Width     = 236,
            Height    = 44,
            BackColor = SidebarBg,
            Cursor    = Cursors.Hand,
            Tag       = item,
            Margin    = Padding.Empty
        };

        var activeBar = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 3,
            BackColor = Color.Transparent
        };

        var icon = new Label
        {
            Text      = item.Icon,
            Font      = new Font("Segoe UI Emoji", 13F),
            ForeColor = SidebarText,
            Dock      = DockStyle.Right,
            Width     = 42,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var title = new Label
        {
            Text      = item.Title,
            Font      = new Font("Tahoma", 10F),
            ForeColor = SidebarText,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 0, 10, 0)
        };

        btn.Controls.Add(title);
        btn.Controls.Add(icon);
        btn.Controls.Add(activeBar);

        void Hover(bool on)
        {
            if (btn == _activeSideBtn) return;
            btn.BackColor = on ? SidebarHover : SidebarBg;
        }

        // ربط الأحداث على كل العناصر الداخلية
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
        // reset القديم
        if (_activeSideBtn != null)
        {
            _activeSideBtn.BackColor = SidebarBg;
            foreach (Control c in _activeSideBtn.Controls)
                if (c.Dock == DockStyle.Right && c.Width == 3) c.BackColor = Color.Transparent;
        }

        // إيجاد الزر الجديد
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
            // تفتيح لون النص
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

    // ════════════════════════════════════════════════════════
    //  MAIN AREA  (Header + Content)
    // ════════════════════════════════════════════════════════
    private Control BuildMainArea()
    {
        var area = new Panel { BackColor = ContentBg };

        // Header — Dock Top
        var header = BuildHeader();
        header.Dock   = DockStyle.Top;
        header.Height = 68;

        // خط فاصل
        var sep = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = Color.FromArgb(190, 205, 220)
        };

        // Content wrapper
        var contentWrap = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = ContentBg,
            Padding   = new Padding(18, 14, 18, 14)
        };

        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White
        };
        card.Paint += (s, e) =>
        {
            using var p = new Pen(Color.FromArgb(200, 215, 232), 1);
            e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
        };

        _contentHost = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(0)
        };
        card.Controls.Add(_contentHost);
        contentWrap.Controls.Add(card);

        area.Controls.Add(contentWrap);
        area.Controls.Add(sep);
        area.Controls.Add(header);
        return area;
    }

    private Panel BuildHeader()
    {
        var header = new Panel
        {
            BackColor = HeaderBg,
            Padding   = new Padding(20, 0, 20, 0)
        };

        // ── ساعة + تاريخ — يسار ──────────────────────────────
        var clockWrap = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 220,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 8, 0, 8)
        };

        _clockLabel = new Label
        {
            Text      = "",
            Font      = new Font("Consolas", 20F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _dateLabel = new Label
        {
            Text      = "",
            Font      = new Font("Tahoma", 8.5F),
            ForeColor = Color.FromArgb(170, 210, 255),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        };

        clockWrap.Controls.Add(_dateLabel);
        clockWrap.Controls.Add(_clockLabel);

        // ── عنوان الصفحة — يمين ──────────────────────────────
        _pageTitle = new Label
        {
            Text      = "",
            Font      = new Font("Tahoma", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Right,
            Width     = 340,
            TextAlign = ContentAlignment.MiddleRight
        };

        // ── شعار في المنتصف ───────────────────────────────────
        var centerLbl = new Label
        {
            Text      = "Smart Market ERP",
            Font      = new Font("Tahoma", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 200, 250),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ترتيب صحيح: Left ثم Right ثم Fill
        header.Controls.Add(centerLbl);
        header.Controls.Add(_pageTitle);
        header.Controls.Add(clockWrap);
        return header;
    }

    // ════════════════════════════════════════════════════════
    //  CLOCK
    // ════════════════════════════════════════════════════════
    private void TickClock()
    {
        var now = DateTime.Now;
        _clockLabel.Text = now.ToString("HH:mm:ss");
        string[] days = { "الأحد","الاثنين","الثلاثاء","الأربعاء","الخميس","الجمعة","السبت" };
        _dateLabel.Text  = $"{days[(int)now.DayOfWeek]}  {now:dd/MM/yyyy}";
    }

    // ════════════════════════════════════════════════════════
    //  NAVIGATION
    // ════════════════════════════════════════════════════════
    private void NavigateTo(NavItem item)
    {
        _pageTitle.Text = $"{item.Icon}  {item.Title}";
        SetActiveNav(item);
        SetContent(ResolveView(item.Title));
    }

    private Control ResolveView(string title) => title switch
    {
        "الرئيسية"  => BuildDashboard(),
        "المبيعات"  => BuildTabs(
            ("🛒 نقطة البيع",       new PosView()),
            ("👥 العملاء",          new CustomersView()),
            ("🎁 العروض والخصومات", new PromotionsView()),
            ("🔄 المرتجعات",        new SalesReturnsView())),
        "المشتريات" => BuildTabs(
            ("📄 فواتير الشراء", new PurchaseInvoicesView()),
            ("🔄 المرتجعات",     new PurchaseReturnsView()),
            ("🏭 الموردين",      new SuppliersView())),
        "الأصناف"   => new ItemsView    { Dock = DockStyle.Fill },
        "المخزون"   => BuildTabs(
            ("📊 مستوى المخزون", new StockLevelView()),
            ("📦 الجرد",         new InventoryCountView()),
            ("🔄 التحويلات",     new TransferView()),
            ("🗑️ التالف",        new DamageView()),
            ("🏭 المستودعات",    EmbedForm<WarehousesManagerForm>())),
        "الخزينة"   => new TreasuryView  { Dock = DockStyle.Fill },
        "التسعير"   => new PricingView   { Dock = DockStyle.Fill },
        "الباركود"  => new BarcodeView   { Dock = DockStyle.Fill },
        "الإعدادات" => new CategoriesView{ Dock = DockStyle.Fill },
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
            Text      = $"🔧  {title}\n\nهذه الوحدة قيد التطوير",
            Font      = new Font("Tahoma", 13F),
            ForeColor = AppTheme.MutedText,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
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

    // ════════════════════════════════════════════════════════
    //  LOGOUT
    // ════════════════════════════════════════════════════════
    private void Logout()
    {
        if (MessageBox.Show("هل تريد تسجيل الخروج؟", "تسجيل الخروج",
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
