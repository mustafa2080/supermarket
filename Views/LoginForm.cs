using supermarket.Data.Repositories;
using supermarket.Services;
using supermarket.Theme;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace supermarket.Views;

internal sealed class LoginForm : Form
{
    private readonly TextBox  _usernameBox  = new();
    private readonly TextBox  _passwordBox  = new();
    private readonly Button   _loginButton  = new();
    private readonly Label    _errorLabel   = new();
    private readonly Label    _attemptsLabel= new();
    private readonly CheckBox _showPassCheck= new();
    private readonly Label    _capsLockLabel= new();

    private int      _failedAttempts = 0;
    private System.Windows.Forms.Timer? _lockTimer;
    private DateTime _lockUntil;
    private SplitContainer? _splitContainer;

    // ── ألوان ────────────────────────────────────────────────
    private static readonly Color DarkBg     = ColorTranslator.FromHtml("#0A1929");
    private static readonly Color DarkPanel  = ColorTranslator.FromHtml("#0D2137");
    private static readonly Color AccentBlue = ColorTranslator.FromHtml("#1565C0");
    private static readonly Color AccentGold = ColorTranslator.FromHtml("#F39C12");
    private static readonly Color LightBg    = ColorTranslator.FromHtml("#F8FAFD");
    private static readonly Color CardBg     = Color.White;
    private static readonly Color TextDark   = ColorTranslator.FromHtml("#1E293B");
    private static readonly Color TextMuted  = ColorTranslator.FromHtml("#64748B");
    private static readonly Color BorderClr  = ColorTranslator.FromHtml("#E2E8F0");
    private static readonly Color BtnPrimary = ColorTranslator.FromHtml("#1B4F72");
    private static readonly Color BtnHover   = ColorTranslator.FromHtml("#2E86C1");
    private static readonly Color SoftBlue   = ColorTranslator.FromHtml("#EAF4FF");
    private static readonly Color Mint       = ColorTranslator.FromHtml("#12B981");
    private static readonly Color DeepNavy   = ColorTranslator.FromHtml("#081521");

    public LoginForm()
    {
        Text              = "Smart Market ERP — تسجيل الدخول";
        Size              = new Size(1360, 820);
        MinimumSize       = new Size(1200, 760);
        StartPosition     = FormStartPosition.CenterScreen;
        FormBorderStyle   = FormBorderStyle.Sizable;
        MaximizeBox       = true;
        WindowState       = FormWindowState.Maximized;
        BackColor         = DarkBg;
        RightToLeft       = RightToLeft.No;
        RightToLeftLayout = false;
        Opacity           = 0;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        UpdateStyles();

        BuildUI();
        WireEvents();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_COMPOSITED = 0x02000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_COMPOSITED;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        BeginInvoke(new Action(() =>
        {
            UpdateSplitLayout();
            PerformLayout();
            if (_splitContainer is not null)
            {
                _splitContainer.Visible = true;
                _splitContainer.Refresh();
            }

            BeginInvoke(new Action(() =>
            {
                Refresh();
                Opacity = 1;
                _usernameBox.Focus();
                _usernameBox.SelectAll();
            }));
        }));
    }

    private void WireEvents()
    {
        _loginButton.Click   += (_, _) => TryLogin();
        _passwordBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };
        _usernameBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) _passwordBox.Focus(); };
        _showPassCheck.CheckedChanged += (_, _) =>
            _passwordBox.UseSystemPasswordChar = !_showPassCheck.Checked;
        _passwordBox.KeyUp += (_, _) => CheckCapsLock();
        _passwordBox.Enter += (_, _) => CheckCapsLock();
        _passwordBox.Leave += (_, _) => _capsLockLabel.Visible = false;
        _loginButton.MouseEnter += (_, _) => { if (_loginButton.Enabled) _loginButton.BackColor = BtnHover; };
        _loginButton.MouseLeave += (_, _) => { if (_loginButton.Enabled) _loginButton.BackColor = BtnPrimary; };
    }

    private void CheckCapsLock() =>
        _capsLockLabel.Visible = Control.IsKeyLocked(Keys.CapsLock);

    // ════════════════════════════════════════════════════════
    //  BUILD UI
    // ════════════════════════════════════════════════════════
    private void BuildUI()
    {
        SuspendLayout();

        var split = new SplitContainer
        {
            Dock            = DockStyle.Fill,
            FixedPanel      = FixedPanel.None,
            IsSplitterFixed = true,
            BorderStyle     = BorderStyle.None,
            BackColor       = DarkPanel,
            SplitterWidth   = 1
        };
        _splitContainer = split;
        split.Visible = false;

        var brand = BuildBrandPanel();
        brand.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(brand);
        split.Panel1.BackColor = DarkPanel;

        var form = BuildFormPanel();
        form.Dock = DockStyle.Fill;
        split.Panel2.Controls.Add(form);
        split.Panel2.BackColor = ColorTranslator.FromHtml("#F8FAFD");

        Controls.Add(split);
        EnableDoubleBufferingRecursive(split);

        this.SizeChanged += (_, _) =>
        {
            UpdateSplitLayout();
        };

        ResumeLayout(true);
    }

    private void UpdateSplitLayout()
    {
        if (_splitContainer is null || _splitContainer.Width <= 0) return;

        try
        {
            _splitContainer.Panel1MinSize = 460;
            _splitContainer.Panel2MinSize = 560;

            int min = _splitContainer.Panel1MinSize;
            int max = _splitContainer.Width - _splitContainer.Panel2MinSize - _splitContainer.SplitterWidth;
            if (max <= min) return;

            int desiredLeft = (int)((_splitContainer.Width - _splitContainer.SplitterWidth) * 0.54);
            int safe = Math.Max(min, Math.Min(desiredLeft, max));
            if (_splitContainer.SplitterDistance != safe)
                _splitContainer.SplitterDistance = safe;
        }
        catch
        {
        }
    }

    private static void EnableDoubleBufferingRecursive(Control control)
    {
        try
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true);
        }
        catch
        {
        }

        foreach (Control child in control.Controls)
            EnableDoubleBufferingRecursive(child);
    }

    // ════════════════════════════════════════════════════════
    //  الجانب الأيمن — Brand Panel
    // ════════════════════════════════════════════════════════
    private Panel BuildBrandPanel()
    {
        var pnl = new Panel { BackColor = DarkPanel };
        pnl.Paint += (_, e) =>
        {
            var rect = pnl.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var gradient = new LinearGradientBrush(
                rect,
                ColorTranslator.FromHtml("#0A1929"),
                ColorTranslator.FromHtml("#12395B"),
                LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(gradient, rect);

            using var glowA = new SolidBrush(Color.FromArgb(45, 21, 101, 192));
            using var glowB = new SolidBrush(Color.FromArgb(35, 243, 156, 18));
            using var glowC = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
            e.Graphics.FillEllipse(glowA, rect.Width - 240, 40, 260, 260);
            e.Graphics.FillEllipse(glowB, -80, rect.Height - 220, 260, 260);
            e.Graphics.FillEllipse(glowC, rect.Width - 140, rect.Height - 180, 120, 120);
        };

        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 5,
            BackColor = AccentGold
        };

        var center = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(72, 48, 72, 42)
        };

        var statusPill = BuildGlassPill("جاهز للعمل", "تشغيل مستقر ومزامنة سلسة", AccentGold);
        statusPill.Dock = DockStyle.Top;

        var logoIcon = new Label
        {
            Text      = "SM",
            Font      = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 88,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        logoIcon.Paint += (_, e) =>
        {
            var r = new Rectangle(logoIcon.Width / 2 - 42, 8, 84, 72);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new LinearGradientBrush(r, AccentGold, BtnHover, LinearGradientMode.ForwardDiagonal);
            using var border = new Pen(Color.FromArgb(90, 255, 255, 255), 1.4f);
            using var path = CreateRoundedPath(r, 26);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(
                e.Graphics,
                "SM",
                new Font("Segoe UI", 22F, FontStyle.Bold),
                r,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var appTitle = new Label
        {
            Text      = "Smart Market ERP",
            Font      = new Font("Tahoma", 28F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 58,
            TextAlign = ContentAlignment.BottomRight
        };

        var appSub = new Label
        {
            Text      = "منصة تشغيل يومية للمبيعات والمخزون والخزينة في واجهة أنيقة وواضحة.",
            Font      = new Font("Tahoma", 12F),
            ForeColor = Color.FromArgb(194, 219, 244),
            Dock      = DockStyle.Top,
            Height    = 52,
            TextAlign = ContentAlignment.TopRight
        };

        var headline = new Label
        {
            Text      = "كل يوم تشغيل يبدأ من شاشة تعطي انطباعًا قويًا وواضحًا.",
            Font      = new Font("Tahoma", 20F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 72,
            TextAlign = ContentAlignment.BottomRight
        };

        var divWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 10, 0, 10)
        };
        divWrap.Controls.Add(new Panel
        {
            Dock      = DockStyle.Fill,
            Height    = 1,
            BackColor = Color.FromArgb(65, 255, 255, 255)
        });

        var features = BuildFeaturesList();
        features.Dock = DockStyle.Top;

        var metrics = BuildMetricsStrip();
        metrics.Dock = DockStyle.Top;

        var copy = new Label
        {
            Text      = "© 2026 Smart Market ERP",
            Font      = new Font("Tahoma", 8F),
            ForeColor = Color.FromArgb(136, 171, 205),
            Dock      = DockStyle.Bottom,
            Height    = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var spacer = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };

        center.Controls.Add(copy);
        center.Controls.Add(features);
        center.Controls.Add(divWrap);
        center.Controls.Add(headline);
        center.Controls.Add(spacer);
        center.Controls.Add(appSub);
        center.Controls.Add(appTitle);
        center.Controls.Add(logoIcon);
        center.Controls.Add(statusPill);

        var topSpace = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };
        center.Controls.Add(topSpace);

        center.Resize += (_, _) =>
        {
            metrics.Visible = center.ClientSize.Height >= 760;
            if (metrics.Visible && !center.Controls.Contains(metrics))
                center.Controls.Add(metrics);
            else if (!metrics.Visible && center.Controls.Contains(metrics))
                center.Controls.Remove(metrics);
        };

        pnl.Controls.Add(center);
        pnl.Controls.Add(topBar);
        return pnl;
    }

    private static Panel BuildFeaturesList()
    {
        var list = new FlowLayoutPanel
        {
            AutoSize      = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(0, 14, 0, 0)
        };

        var items = new[]
        {
            "تجربة تشغيل أسرع مع وصول مباشر للمهام اليومية",
            "متابعة دقيقة للمخزون والحركة داخل المستودعات",
            "تناغم بصري يرفع الثقة منذ لحظة تسجيل الدخول",
            "واجهة جاهزة للتوسع مع بقية وحدات النظام"
        };

        foreach (var txt in items)
        {
            var row = new Panel
            {
                AutoSize  = true,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 6, 0, 0),
                Padding   = new Padding(16, 10, 16, 10),
                Width     = 360,
                Height    = 44
            };

            row.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, row.Width - 1, row.Height - 1);
                using var path = CreateRoundedPath(rect, 18);
                using var b = new SolidBrush(Color.FromArgb(26, 255, 255, 255));
                using var p = new Pen(Color.FromArgb(44, 255, 255, 255));
                e.Graphics.FillPath(b, path);
                e.Graphics.DrawPath(p, path);
            };

            var checkIcon = new Label
            {
                Text      = "●",
                Font      = new Font("Tahoma", 11F, FontStyle.Bold),
                ForeColor = AccentGold,
                Size      = new Size(22, 22),
                Location  = new Point(row.Width - 38, 11),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var textLbl = new Label
            {
                Text      = txt,
                Font      = new Font("Tahoma", 10F),
                ForeColor = Color.FromArgb(222, 239, 255),
                AutoSize  = false,
                Size      = new Size(290, 22),
                Location  = new Point(18, 11),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            row.Controls.Add(checkIcon);
            row.Controls.Add(textLbl);
            list.Controls.Add(row);
        }

        return list;
    }

    private static Panel BuildMetricsStrip()
    {
        var wrap = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 74,
            ColumnCount = 3,
            RightToLeft = RightToLeft.Yes,
            BackColor   = Color.Transparent,
            Margin      = new Padding(0),
            Padding     = new Padding(0, 12, 0, 0)
        };

        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        wrap.Controls.Add(BuildMetricCard("POS", "تشغيل سريع", 0), 0, 0);
        wrap.Controls.Add(BuildMetricCard("24/7", "جاهزية مستمرة", 1), 1, 0);
        wrap.Controls.Add(BuildMetricCard("ERP", "وحدات مترابطة", 2), 2, 0);

        return wrap;
    }

    private static Panel BuildMetricCard(string value, string caption, int column)
    {
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            Margin    = new Padding(column == 0 ? 0 : 6, 0, column == 2 ? 0 : 6, 0),
            BackColor = Color.Transparent
        };

        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using var path = CreateRoundedPath(rect, 18);
            using var fill = new SolidBrush(Color.FromArgb(18, 255, 255, 255));
            using var border = new Pen(Color.FromArgb(40, 255, 255, 255));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        };

        var valueLbl = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            Text      = value,
            Font      = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.BottomCenter
        };

        var captionLbl = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = caption,
            Font      = new Font("Tahoma", 8.8F),
            ForeColor = Color.FromArgb(191, 214, 236),
            TextAlign = ContentAlignment.TopCenter
        };

        card.Controls.Add(captionLbl);
        card.Controls.Add(valueLbl);
        return card;
    }

    // ════════════════════════════════════════════════════════
    //  الجانب الأيسر — Form Panel
    // ════════════════════════════════════════════════════════
    private Panel BuildFormPanel()
    {
        var outer = new Panel { BackColor = LightBg };

        outer.Paint += (_, e) =>
        {
            var rect = outer.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new LinearGradientBrush(
                rect,
                ColorTranslator.FromHtml("#F7FBFF"),
                ColorTranslator.FromHtml("#EEF5FB"),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(bg, rect);

            using var blobA = new SolidBrush(Color.FromArgb(95, 227, 242, 253));
            using var blobB = new SolidBrush(Color.FromArgb(70, 214, 230, 245));
            using var blobC = new SolidBrush(Color.FromArgb(75, 255, 243, 224));
            e.Graphics.FillEllipse(blobA, rect.Width - 220, 48, 180, 180);
            e.Graphics.FillEllipse(blobB, 24, rect.Height - 180, 200, 200);
            e.Graphics.FillEllipse(blobC, rect.Width - 120, rect.Height - 140, 120, 120);
        };

        var card = BuildLoginCard();

        void PositionCard()
        {
            int cardW = Math.Min(760, outer.ClientSize.Width - 56);
            int cardH = Math.Min(820, outer.ClientSize.Height - 48);
            cardW = Math.Max(560, cardW);
            cardH = Math.Max(620, cardH);

            card.Size     = new Size(cardW, cardH);
            card.Location = new Point(
                (outer.ClientSize.Width  - cardW) / 2,
                (outer.ClientSize.Height - cardH) / 2
            );
        }

        outer.Controls.Add(card);
        outer.Resize += (_, _) => PositionCard();
        outer.Layout += (_, _) => PositionCard();

        return outer;
    }

    private Panel BuildLoginCard()
    {
        var card = new Panel
        {
            BackColor = CardBg,
            Padding   = new Padding(48, 40, 48, 34)
        };

        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 10; i >= 1; i--)
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb(i * 4, 15, 54, 88));
                using var shadowPath = CreateRoundedPath(new Rectangle(i, i + 2, card.Width - (i * 2) - 1, card.Height - (i * 2) - 1), 30);
                g.FillPath(shadowBrush, shadowPath);
            }

            var cardRect = new Rectangle(0, 0, card.Width - 12, card.Height - 12);
            using var cardPath = CreateRoundedPath(cardRect, 30);
            using var bgBrush = new SolidBrush(CardBg);
            using var pen = new Pen(Color.FromArgb(220, BorderClr), 1);
            g.FillPath(bgBrush, cardPath);
            g.DrawPath(pen, cardPath);

            using var topGlow = new LinearGradientBrush(
                new Rectangle(0, 0, card.Width - 12, 8),
                BtnHover,
                AccentGold,
                LinearGradientMode.Horizontal);
            g.FillRectangle(topGlow, 0, 0, card.Width - 12, 6);
        };

        var titleWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 188,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 16, 0, 10)
        };

        var accessLbl = new Label
        {
            Text      = "تسجيل دخول النظام",
            Font      = new Font("Tahoma", 13F, FontStyle.Bold),
            ForeColor = BtnPrimary,
            Dock      = DockStyle.Top,
            Height    = 32,
            TextAlign = ContentAlignment.BottomRight
        };

        var welcomeLbl = new Label
        {
            Text      = "مرحبًا بعودتك",
            Font      = new Font("Tahoma", 26F, FontStyle.Bold),
            ForeColor = TextDark,
            Dock      = DockStyle.Top,
            Height    = 62,
            TextAlign = ContentAlignment.BottomRight
        };
        var subLbl = new Label
        {
            Text      = "أدخل بياناتك للوصول إلى لوحة التحكم وإدارة العمليات اليومية.",
            Font      = new Font("Tahoma", 11F),
            ForeColor = TextMuted,
            Dock      = DockStyle.Top,
            Height    = 46,
            TextAlign = ContentAlignment.TopRight
        };
        var subInfo = new Label
        {
            Text      = "تشفير كلمة المرور وتسجيل الدخول في سجل التدقيق مفعلان.",
            Font      = new Font("Tahoma", 10F),
            ForeColor = ColorTranslator.FromHtml("#4B6B88"),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopRight
        };
        titleWrap.Controls.Add(subInfo);
        titleWrap.Controls.Add(subLbl);
        titleWrap.Controls.Add(welcomeLbl);
        titleWrap.Controls.Add(accessLbl);

        var divider = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = BorderClr
        };

        var fieldsPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 332,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 24, 0, 0)
        };

        var sectionLabel = new Label
        {
            Text      = "بيانات تسجيل الدخول",
            Font      = new Font("Tahoma", 12F, FontStyle.Bold),
            ForeColor = BtnPrimary,
            AutoSize  = false,
            Size      = new Size(220, 24),
            Location  = new Point(0, 0),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };

        var userGroup = BuildFieldGroup("اسم المستخدم", "👤", _usernameBox, "admin");
        userGroup.Top  = 44;
        userGroup.Left = 18;
        userGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _passwordBox.UseSystemPasswordChar = true;
        var passGroup = BuildFieldGroup("كلمة المرور", "🔒", _passwordBox, "••••••••");
        passGroup.Top    = 154;
        passGroup.Left   = 18;
        passGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _capsLockLabel.Text        = "⚠️  CapsLock مفعّل";
        _capsLockLabel.Font        = new Font("Tahoma", 8.5F);
        _capsLockLabel.ForeColor   = Color.FromArgb(150, 100, 0);
        _capsLockLabel.BackColor   = Color.FromArgb(255, 248, 220);
        _capsLockLabel.TextAlign   = ContentAlignment.MiddleRight;
        _capsLockLabel.Padding     = new Padding(0, 0, 10, 0);
        _capsLockLabel.Visible     = false;
        _capsLockLabel.Size        = new Size(340, 28);
        _capsLockLabel.Top         = 268;
        _capsLockLabel.Left        = 18;
        _capsLockLabel.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _capsLockLabel.Paint += (_, e) =>
        {
            var r = new Rectangle(0, 0, _capsLockLabel.Width - 1, _capsLockLabel.Height - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedPath(r, 12);
            using var fill = new SolidBrush(Color.FromArgb(255, 248, 220));
            using var border = new Pen(Color.FromArgb(227, 193, 111));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(
                e.Graphics,
                _capsLockLabel.Text,
                _capsLockLabel.Font,
                r,
                _capsLockLabel.ForeColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        };

        _showPassCheck.Text      = "إظهار كلمة المرور";
        _showPassCheck.Font      = new Font("Tahoma", 9F);
        _showPassCheck.ForeColor = TextMuted;
        _showPassCheck.BackColor = Color.Transparent;
        _showPassCheck.RightToLeft = RightToLeft.Yes;
        _showPassCheck.Size      = new Size(180, 24);
        _showPassCheck.Top       = 300;
        _showPassCheck.Left      = 18;
        _showPassCheck.Anchor    = AnchorStyles.Top | AnchorStyles.Right;

        fieldsPanel.Controls.Add(sectionLabel);
        fieldsPanel.Controls.Add(userGroup);
        fieldsPanel.Controls.Add(passGroup);
        fieldsPanel.Controls.Add(_capsLockLabel);
        fieldsPanel.Controls.Add(_showPassCheck);

        _errorLabel.Dock        = DockStyle.Top;
        _errorLabel.Height      = 0;
        _errorLabel.Font        = new Font("Tahoma", 9.5F);
        _errorLabel.ForeColor   = Color.White;
        _errorLabel.BackColor   = Color.FromArgb(197, 48, 48);
        _errorLabel.TextAlign   = ContentAlignment.MiddleCenter;
        _errorLabel.Visible     = false;
        _errorLabel.Padding     = new Padding(8);

        StyleLoginButton();
        var btnWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 74,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 14, 0, 0)
        };
        _loginButton.Dock = DockStyle.Fill;
        btnWrap.Controls.Add(_loginButton);

        _attemptsLabel.Dock      = DockStyle.Top;
        _attemptsLabel.Height    = 32;
        _attemptsLabel.Font      = new Font("Tahoma", 8.5F);
        _attemptsLabel.ForeColor = TextMuted;
        _attemptsLabel.TextAlign = ContentAlignment.MiddleCenter;
        _attemptsLabel.Text      = "للمساعدة تواصل مع مدير النظام";
        _attemptsLabel.BackColor = Color.Transparent;

        var footerHint = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = new Font("Tahoma", 8.5F),
            ForeColor = ColorTranslator.FromHtml("#7B8DA1"),
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = "تأكد من اختيار لوحة المفاتيح الصحيحة قبل إدخال كلمة المرور."
        };

        var contentFlow = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 12, 0, 0)
        };

        contentFlow.Controls.Add(footerHint);
        contentFlow.Controls.Add(_attemptsLabel);
        contentFlow.Controls.Add(btnWrap);
        contentFlow.Controls.Add(_errorLabel);
        contentFlow.Controls.Add(fieldsPanel);

        card.Controls.Add(contentFlow);
        card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent });
        card.Controls.Add(divider);
        card.Controls.Add(titleWrap);

        card.Layout += (_, _) =>
        {
            int w = card.ClientSize.Width - 132;
            if (w < 100) return;
            userGroup.Width      = w;
            passGroup.Width      = w;
            _capsLockLabel.Width = w;
            sectionLabel.Width   = w;
        };

        return card;
    }

    private Panel BuildFieldGroup(string label, string icon, TextBox tb, string placeholder)
    {
        var group = new Panel
        {
            Height    = 104,
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text      = $"{icon}  {label}",
            Font      = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize  = false,
            Location  = new Point(0, 0),
            Height    = 28,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var hint = new Label
        {
            Text      = label == "اسم المستخدم" ? "اكتب اسم المستخدم المعتمد في النظام" : "اكتب كلمة المرور الخاصة بحسابك",
            Font      = new Font("Tahoma", 9F),
            ForeColor = ColorTranslator.FromHtml("#6E8095"),
            AutoSize  = false,
            Location  = new Point(0, 24),
            Height    = 20,
            TextAlign = ContentAlignment.TopRight,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var inputShell = new Panel
        {
            Size      = new Size(group.Width, 60),
            Location  = new Point(0, 44),
            BackColor = Color.White,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Padding   = new Padding(16, 12, 16, 12)
        };

        inputShell.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, inputShell.Width - 1, inputShell.Height - 1);
            using var path = CreateRoundedPath(rect, 16);
            using var fill = new SolidBrush(tb.Focused ? Color.FromArgb(247, 251, 255) : Color.White);
            using var pen = new Pen(tb.Focused ? BtnHover : Color.FromArgb(198, 211, 224), tb.Focused ? 2f : 1f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
        };

        tb.Font        = new Font("Tahoma", 12F);
        tb.BackColor   = Color.White;
        tb.ForeColor   = TextDark;
        tb.BorderStyle = BorderStyle.None;
        tb.Multiline   = false;
        tb.PlaceholderText = placeholder;
        tb.Size        = new Size(group.Width - 60, 30);
        tb.Location    = new Point(16, 16);
        tb.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        tb.TextAlign   = HorizontalAlignment.Right;

        tb.Enter += (_, _) =>
        {
            inputShell.Invalidate();
        };
        tb.Leave += (_, _) =>
        {
            inputShell.Invalidate();
        };

        group.Layout += (_, _) =>
        {
            inputShell.Width = group.Width;
            tb.Width         = inputShell.Width - 32;
            lbl.Width        = group.Width;
            hint.Width       = group.Width;
        };

        inputShell.Controls.Add(tb);
        group.Controls.Add(inputShell);
        group.Controls.Add(hint);
        group.Controls.Add(lbl);
        return group;
    }

    private void StyleLoginButton()
    {
        _loginButton.Text      = "تسجيل الدخول";
        _loginButton.Font      = new Font("Tahoma", 12F, FontStyle.Bold);
        _loginButton.BackColor = BtnPrimary;
        _loginButton.ForeColor = Color.White;
        _loginButton.FlatStyle = FlatStyle.Flat;
        _loginButton.FlatAppearance.BorderSize  = 0;
        _loginButton.FlatAppearance.MouseOverBackColor = BtnHover;
        _loginButton.Cursor    = Cursors.Hand;
        _loginButton.Height    = 50;
    }

    // ════════════════════════════════════════════════════════
    //  منطق تسجيل الدخول
    // ════════════════════════════════════════════════════════
    private void TryLogin()
    {
        if (_lockTimer != null && DateTime.Now < _lockUntil)
        {
            int remSec = (int)(_lockUntil - DateTime.Now).TotalSeconds;
            ShowError($"الحساب مقفول مؤقتاً — انتظر {remSec} ثانية.");
            return;
        }

        var username = _usernameBox.Text.Trim();
        var password = _passwordBox.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("يرجى إدخال اسم المستخدم وكلمة المرور.");
            return;
        }

        SetLoadingState(true);
        try
        {
            var repo = new UserRepository();
            var user = repo.GetByUsername(username);

            if (user is null) { HandleFailedAttempt(); return; }

            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
            {
                int rem = (int)(user.LockedUntil.Value - DateTime.Now).TotalMinutes + 1;
                ShowError($"الحساب مقفول. حاول مرة أخرى بعد {rem} دقيقة.");
                SetLoadingState(false);
                return;
            }

            if (!user.IsActive)
            {
                ShowError("هذا الحساب معطّل. تواصل مع مدير النظام.");
                SetLoadingState(false);
                return;
            }

            bool ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!ok) { repo.IncrementLoginAttempts(user.Id); HandleFailedAttempt(); return; }

            repo.UpdateLastLogin(user.Id);
            var perms = repo.GetPermissions(user.RoleId);
            SessionContext.StartSession(user, perms);
            AuditService.LogLogin(user.Id);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في الاتصال: {ex.Message}");
            SetLoadingState(false);
        }
    }

    private void HandleFailedAttempt()
    {
        _failedAttempts++;
        int rem = 5 - _failedAttempts;

        if (_failedAttempts >= 5)
        {
            _lockUntil = DateTime.Now.AddMinutes(15);
            ShowError("تم قفل الحساب لمدة 15 دقيقة بسبب تكرار الأخطاء.");
            _attemptsLabel.Text      = "🔒  الحساب مقفول مؤقتاً";
            _attemptsLabel.ForeColor = Color.FromArgb(197, 48, 48);
            _loginButton.Enabled     = false;
            _loginButton.Text        = "⏳  مقفول...";

            _lockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _lockTimer.Tick += (_, _) =>
            {
                if (DateTime.Now >= _lockUntil)
                {
                    _lockTimer!.Stop(); _lockTimer.Dispose(); _lockTimer = null;
                    _failedAttempts = 0;
                    _loginButton.Enabled   = true;
                    StyleLoginButton();
                    _attemptsLabel.Text      = "للمساعدة تواصل مع مدير النظام";
                    _attemptsLabel.ForeColor = TextMuted;
                    HideError();
                }
                else
                {
                    int s = (int)(_lockUntil - DateTime.Now).TotalSeconds;
                    _loginButton.Text = $"⏳  {s}ث";
                }
            };
            _lockTimer.Start();
        }
        else
        {
            ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
            _attemptsLabel.Text      = $"⚠️  محاولات متبقية: {rem}";
            _attemptsLabel.ForeColor = Color.FromArgb(180, 100, 0);
            SetLoadingState(false);
        }

        _passwordBox.Clear();
        _passwordBox.Focus();
    }

    private void ShowError(string msg)
    {
        _errorLabel.Text    = msg;
        _errorLabel.Height  = 42;
        _errorLabel.Visible = true;
    }

    private void HideError()
    {
        _errorLabel.Visible = false;
        _errorLabel.Height  = 0;
    }

    private void SetLoadingState(bool loading)
    {
        _loginButton.Enabled = !loading;
        _loginButton.Text    = loading ? "⏳  جاري التحقق..." : "تسجيل الدخول";
        _loginButton.BackColor = BtnPrimary;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _lockTimer?.Stop();
        _lockTimer?.Dispose();
        base.OnFormClosed(e);
    }

    private static Panel BuildGlassPill(string title, string subtitle, Color accent)
    {
        var pill = new Panel
        {
            Height    = 76,
            BackColor = Color.Transparent
        };

        pill.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pill.Width - 1, pill.Height - 1);
            using var path = CreateRoundedPath(rect, 24);
            using var fill = new SolidBrush(Color.FromArgb(22, 255, 255, 255));
            using var border = new Pen(Color.FromArgb(54, 255, 255, 255));
            using var accentBrush = new SolidBrush(accent);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            e.Graphics.FillEllipse(accentBrush, pill.Width - 52, 22, 10, 10);
        };

        var titleLbl = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 34,
            Text      = title,
            Font      = new Font("Tahoma", 11F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.BottomRight
        };

        var subLbl = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = subtitle,
            Font      = new Font("Tahoma", 8.8F),
            ForeColor = Color.FromArgb(200, 223, 244),
            TextAlign = ContentAlignment.TopRight
        };

        pill.Controls.Add(subLbl);
        pill.Controls.Add(titleLbl);
        return pill;
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (rect.Width <= 0 || rect.Height <= 0) return path;

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
