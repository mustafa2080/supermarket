using System.Drawing;
using supermarket.Services;
using supermarket.Theme;
using supermarket.Views;

namespace supermarket;

public sealed class MainForm : Form
{
    private Label _pageTitle = null!;
    private Label _pageDescription = null!;
    private Panel _contentHost = null!;
    private readonly StatusStrip _statusStrip;

    public MainForm()
    {
        Text = "Smart Market ERP";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1200, 760);
        WindowState = FormWindowState.Maximized;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppTheme.Background
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

        mainLayout.Controls.Add(BuildHeader(), 0, 0);
        mainLayout.Controls.Add(BuildNavigationBar(), 0, 1);
        mainLayout.Controls.Add(BuildBody(), 0, 2);

        _statusStrip = BuildStatusBar();
        mainLayout.Controls.Add(_statusStrip, 0, 3);

        Controls.Add(mainLayout);
        InitializeDashboard();
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Primary,
            Padding = new Padding(24, 18, 24, 18)
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont,
            ForeColor = Color.White,
            Text = "Smart Market ERP",
            TextAlign = ContentAlignment.MiddleRight
        };

        var subtitle = new Label
        {
            Dock = DockStyle.Left,
            Width = 360,
            Font = AppTheme.SmallFont,
            ForeColor = Color.WhiteSmoke,
            Text = "نظام إدارة السوبر ماركت الذكي",
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        return header;
    }

    private Control BuildNavigationBar()
    {
        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Background,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(18, 14, 18, 10),
            WrapContents = false
        };

        navigation.Controls.Add(CreateNavButton("الإعدادات", "إدارة البيانات الأساسية والإعدادات العامة"));
        navigation.Controls.Add(CreateNavButton("التقارير", "نظرة سريعة على التقارير والتحليلات"));
        navigation.Controls.Add(CreateNavButton("الخزينة", "متابعة الخزائن والورديات والسندات"));
        navigation.Controls.Add(CreateNavButton("المخزون", "مراقبة الأرصدة وحركات المستودعات"));
        navigation.Controls.Add(CreateNavButton("المشتريات", "تسجيل الموردين وفواتير الشراء"));
        navigation.Controls.Add(CreateNavButton("الأصناف", "بطاقة الصنف وإدارة بيانات الأصناف الأساسية"));
        navigation.Controls.Add(CreateNavButton("المبيعات", "تشغيل نقطة البيع وإدارة العملاء"));

        return navigation;
    }

    private Button CreateNavButton(string title, string description)
    {
        var button = new Button
        {
            Text = title,
            Width = 160,
            Margin = new Padding(8, 0, 8, 0),
            Tag = description
        };

        AppTheme.StyleSecondaryButton(button);
        button.Click += (_, _) => NavigateTo(title, description);
        return button;
    }

    private Control BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18, 8, 18, 18),
            BackColor = AppTheme.Background
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 126F));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var summaryCard = AppTheme.CreateCard();
        summaryCard.Dock = DockStyle.Fill;

        _pageTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 38,
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.DarkText,
            TextAlign = ContentAlignment.MiddleRight
        };

        _pageDescription = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.TopRight
        };

        summaryCard.Controls.Add(_pageDescription);
        summaryCard.Controls.Add(_pageTitle);

        var contentCard = AppTheme.CreateCard();
        contentCard.Dock = DockStyle.Fill;

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface
        };

        contentCard.Controls.Add(_contentHost);

        body.Controls.Add(summaryCard, 0, 0);
        body.Controls.Add(contentCard, 0, 1);
        return body;
    }

    private StatusStrip BuildStatusBar()
    {
        var strip = new StatusStrip
        {
            RightToLeft = RightToLeft.Yes,
            SizingGrip = false,
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            Font = AppTheme.SmallFont
        };

        strip.Items.Add(new ToolStripStatusLabel($"المستخدم: {SessionContext.DisplayName}  ({SessionContext.RoleAr})"));
        strip.Items.Add(new ToolStripStatusLabel(" | "));
        strip.Items.Add(new ToolStripStatusLabel("الوردية: غير مفتوحة"));
        strip.Items.Add(new ToolStripStatusLabel(" | "));
        strip.Items.Add(new ToolStripStatusLabel($"التاريخ: {DateTime.Now:dd/MM/yyyy}"));
        strip.Items.Add(new ToolStripStatusLabel("                              "));

        var logoutLabel = new ToolStripStatusLabel("🔓 تسجيل خروج")
        {
            ForeColor  = Color.LightYellow,
            IsLink     = true,
            LinkBehavior = LinkBehavior.AlwaysUnderline
        };
        logoutLabel.Click += (_, _) => Logout();
        strip.Items.Add(logoutLabel);

        return strip;
    }

    private void InitializeDashboard()
    {
        var dashboard = new DashboardView();
        dashboard.ModuleRequested += (title, description) => NavigateTo(title, description);

        ShowSection(
            "لوحة البداية",
            "تم تجهيز الهيكل الأساسي للتطبيق طبقاً للـ PRD: تنقل بين الوحدات، دعم RTL، ونظام ألوان موحد. الخطوة التالية ستكون تحويل كل وحدة إلى شاشات عمل فعلية وربطها بطبقة البيانات."
        );
        SetContent(dashboard);
    }

    private void ShowSection(string title, string description)
    {
        _pageTitle.Text = title;
        _pageDescription.Text = description;
    }

    private void NavigateTo(string title, string description)
    {
        ShowSection(title, description);

        if (title == "المبيعات")
        {
            SetContent(new PosView());
            return;
        }

        if (title == "الأصناف" || title == "بطاقة الصنف")
        {
            SetContent(new ItemsView());
            return;
        }

        if (title == "الإعدادات")
        {
            SetContent(new CategoriesView());
            return;
        }

        if (title == "المشتريات")
        {
            // عرض تاب مزدوج: الموردين + فواتير الشراء
            var tabCtrl = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
            var tabInvoices  = new TabPage("📄 فواتير الشراء")  { BackColor = AppTheme.Surface };
            var tabReturns   = new TabPage("🔄 المرتجعات")       { BackColor = AppTheme.Surface };
            var tabSuppliers = new TabPage("🏭 الموردين")        { BackColor = AppTheme.Surface };

            var invView = new PurchaseInvoicesView { Dock = DockStyle.Fill };
            var retView = new PurchaseReturnsView  { Dock = DockStyle.Fill };
            var supView = new SuppliersView        { Dock = DockStyle.Fill };

            tabInvoices.Controls.Add(invView);
            tabReturns.Controls.Add(retView);
            tabSuppliers.Controls.Add(supView);

            tabCtrl.TabPages.Add(tabInvoices);
            tabCtrl.TabPages.Add(tabReturns);
            tabCtrl.TabPages.Add(tabSuppliers);
            SetContent(tabCtrl);
            return;
        }

        if (title == "المستخدمون" || title == "إدارة المستخدمين")
        {
            if (!SessionContext.IsAdmin)
            {
                ShowSection(title, "ليس لديك صلاحية لإدارة المستخدمين.");
                SetContent(CreatePlaceholderView(title, "هذه الوحدة متاحة لمدير النظام فقط."));
                return;
            }
            SetContent(new UsersView());
            return;
        }

        SetContent(CreatePlaceholderView(title, description));
    }

    private void SetContent(Control control)
    {
        _contentHost.Controls.Clear();
        control.Dock = DockStyle.Fill;
        _contentHost.Controls.Add(control);
    }

    private void Logout()
    {
        var confirm = MessageBox.Show(
            "هل تريد تسجيل الخروج؟",
            "تسجيل الخروج",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        if (SessionContext.CurrentUser is not null)
            AuditService.LogLogout(SessionContext.CurrentUser.Id);

        SessionContext.EndSession();

        // فتح شاشة Login مرة أخرى
        Hide();
        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK)
        {
            // تسجيل دخول ناجح — أعد تحميل الشاشة الرئيسية
            Show();
            InitializeDashboard();
        }
        else
        {
            Application.Exit();
        }
    }

    private Control CreatePlaceholderView(string title, string description)
    {
        var panel = new Panel
        {
            BackColor = AppTheme.Surface
        };

        var infoCard = AppTheme.CreateCard();
        infoCard.Dock = DockStyle.Top;
        infoCard.Height = 220;

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 42,
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary,
            Text = title,
            TextAlign = ContentAlignment.MiddleRight
        };

        var descriptionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 80,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.MutedText,
            Text = description,
            TextAlign = ContentAlignment.TopRight
        };

        var noteLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.DarkText,
            Text = "هذه الوحدة ستكون التالية في التنفيذ. تم الإبقاء عليها كمساحة جاهزة حتى نحافظ على التنقل موحداً داخل النظام.",
            TextAlign = ContentAlignment.TopRight
        };

        infoCard.Controls.Add(noteLabel);
        infoCard.Controls.Add(descriptionLabel);
        infoCard.Controls.Add(titleLabel);

        panel.Controls.Add(infoCard);
        return panel;
    }
}
