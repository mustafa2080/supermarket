using supermarket.Data.Repositories;
using supermarket.Services;
using supermarket.Theme;

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

    public LoginForm()
    {
        Text              = "Smart Market ERP — تسجيل الدخول";
        Size              = new Size(1100, 700);
        MinimumSize       = new Size(960, 620);
        StartPosition     = FormStartPosition.CenterScreen;
        FormBorderStyle   = FormBorderStyle.Sizable;
        MaximizeBox       = true;
        BackColor         = DarkBg;
        RightToLeft       = RightToLeft.No;
        RightToLeftLayout = false;

        BuildUI();
        WireEvents();
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
        var split = new SplitContainer
        {
            Dock            = DockStyle.Fill,
            FixedPanel      = FixedPanel.Panel1,
            IsSplitterFixed = true,
            BorderStyle     = BorderStyle.None,
            BackColor       = DarkPanel,
            SplitterWidth   = 1
        };

        // Panel1 = يسار = Brand داكن
        var brand = BuildBrandPanel();
        brand.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(brand);
        split.Panel1.BackColor = DarkPanel;

        // Panel2 = يمين = Form فاتح
        var form = BuildFormPanel();
        form.Dock = DockStyle.Fill;
        split.Panel2.Controls.Add(form);
        split.Panel2.BackColor = ColorTranslator.FromHtml("#F8FAFD");

        Controls.Add(split);

        // نضبط الأبعاد بعد ما الـ Form يظهر فعلاً
        this.Shown += (_, _) =>
        {
            try
            {
                split.Panel1MinSize   = 280;
                split.Panel2MinSize   = 340;
                split.SplitterDistance = Math.Max(280, (int)(split.Width * 0.42));
            }
            catch { }
        };
    }

    // ════════════════════════════════════════════════════════
    //  الجانب الأيمن — Brand Panel
    // ════════════════════════════════════════════════════════
    private Panel BuildBrandPanel()
    {
        var pnl = new Panel { BackColor = DarkPanel };

        // شريط ذهبي أعلى
        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 5,
            BackColor = AccentGold
        };

        // منطقة المحتوى المركزية
        var center = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(50, 0, 50, 0)
        };

        // أيقونة + عنوان
        var logoIcon = new Label
        {
            Text      = "🛒",
            Font      = new Font("Segoe UI Emoji", 58F),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 90,
            TextAlign = ContentAlignment.BottomCenter
        };

        var appTitle = new Label
        {
            Text      = "Smart Market ERP",
            Font      = new Font("Tahoma", 22F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 44,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var appSub = new Label
        {
            Text      = "نظام إدارة السوبر ماركت الذكي",
            Font      = new Font("Tahoma", 11F),
            ForeColor = Color.FromArgb(160, 200, 240),
            Dock      = DockStyle.Top,
            Height    = 30,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // خط فاصل ذهبي
        var divider = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 2,
            BackColor = Color.FromArgb(60, AccentGold),
            Margin    = new Padding(40, 0, 40, 0)
        };
        var divWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = Color.Transparent,
            Padding   = new Padding(60, 8, 60, 8)
        };
        divWrap.Controls.Add(new Panel
        {
            Dock      = DockStyle.Fill,
            Height    = 1,
            BackColor = Color.FromArgb(70, 255, 255, 255)
        });

        // مميزات
        var features = BuildFeaturesList();
        features.Dock = DockStyle.Top;

        // Copyright أسفل
        var copy = new Label
        {
            Text      = "© 2025 Smart Market — جميع الحقوق محفوظة",
            Font      = new Font("Tahoma", 8F),
            ForeColor = Color.FromArgb(80, 130, 180),
            Dock      = DockStyle.Bottom,
            Height    = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Spacer مرن
        var spacer = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.Transparent };

        center.Controls.Add(copy);
        center.Controls.Add(features);
        center.Controls.Add(divWrap);
        center.Controls.Add(spacer);
        center.Controls.Add(appSub);
        center.Controls.Add(appTitle);
        center.Controls.Add(logoIcon);

        // فراغ علوي
        var topSpace = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
        center.Controls.Add(topSpace);

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
            Padding       = new Padding(0, 18, 0, 0)
        };

        var items = new[]
        {
            "إدارة متكاملة للمبيعات ونقاط البيع",
            "مراقبة المخزون والمستودعات",
            "الخزينة والورديات وسندات الصرف",
            "التسعير والباركود والتقارير"
        };

        foreach (var txt in items)
        {
            var row = new Panel
            {
                AutoSize  = true,
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 6, 0, 0),
                Padding   = new Padding(14, 8, 14, 8),
                Width     = 340
            };

            row.Paint += (_, e) =>
            {
                using var b = new SolidBrush(Color.FromArgb(25, 255, 255, 255));
                e.Graphics.FillRectangle(b, 0, 0, row.Width, row.Height);
                using var p = new Pen(Color.FromArgb(40, 255, 255, 255));
                // رسم border يدوي لأن Panel لا يدعم BorderStyle مع BackColor
                e.Graphics.DrawRectangle(p, 0, 0, row.Width - 1, row.Height - 1);
            };

            var checkIcon = new Label
            {
                Text      = "✓",
                Font      = new Font("Tahoma", 11F, FontStyle.Bold),
                ForeColor = AccentGold,
                AutoSize  = true,
                Location  = new Point(row.Width - 28, 8),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            var textLbl = new Label
            {
                Text      = txt,
                Font      = new Font("Tahoma", 10F),
                ForeColor = Color.FromArgb(210, 235, 255),
                AutoSize  = true,
                Location  = new Point(14, 9),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left
            };

            row.Controls.Add(checkIcon);
            row.Controls.Add(textLbl);
            row.Height = 36;
            list.Controls.Add(row);
        }

        return list;
    }

    // ════════════════════════════════════════════════════════
    //  الجانب الأيسر — Form Panel
    // ════════════════════════════════════════════════════════
    private Panel BuildFormPanel()
    {
        var outer = new Panel { BackColor = LightBg };

        // ظل خفيف على الحافة اليسرى
        outer.Paint += (_, e) =>
        {
            for (int i = 0; i < 20; i++)
            {
                using var b = new SolidBrush(Color.FromArgb(12 - i / 2, 0, 0, 0));
                e.Graphics.FillRectangle(b, i, 0, 1, outer.Height);
            }
        };

        // بطاقة تسجيل الدخول في المنتصف
        var cardHost = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        var card = BuildLoginCard();
        cardHost.Controls.Add(card);
        cardHost.Layout += (_, _) =>
        {
            int cardW = Math.Min(420, cardHost.Width - 60);
            int cardH = Math.Min(560, cardHost.Height - 40);
            card.Size     = new Size(cardW, cardH);
            card.Location = new Point(
                (cardHost.Width  - cardW) / 2,
                (cardHost.Height - cardH) / 2);
        };

        outer.Controls.Add(cardHost);
        return outer;
    }

    private Panel BuildLoginCard()
    {
        var card = new Panel
        {
            BackColor = CardBg,
            Padding   = new Padding(40, 32, 40, 32)
        };

        card.Paint += (_, e) =>
        {
            // ظل خفيف
            using var pen = new Pen(Color.FromArgb(180, BorderClr), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);

            // شريط أعلى أزرق داكن
            using var b = new SolidBrush(BtnPrimary);
            e.Graphics.FillRectangle(b, 0, 0, card.Width, 5);
        };

        // ── عنوان ────────────────────────────────────────────
        var titleWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 72,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 12, 0, 8)
        };

        var welcomeLbl = new Label
        {
            Text      = "مرحباً بك  👋",
            Font      = new Font("Tahoma", 20F, FontStyle.Bold),
            ForeColor = TextDark,
            Dock      = DockStyle.Top,
            Height    = 40,
            TextAlign = ContentAlignment.BottomRight
        };
        var subLbl = new Label
        {
            Text      = "سجّل دخولك للمتابعة إلى لوحة التحكم",
            Font      = new Font("Tahoma", 9.5F),
            ForeColor = TextMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopRight
        };
        titleWrap.Controls.Add(subLbl);
        titleWrap.Controls.Add(welcomeLbl);

        // فاصل
        var divider = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 1,
            BackColor = BorderClr
        };

        // ── حقول ─────────────────────────────────────────────
        var fieldsPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 260,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 18, 0, 0)
        };

        // حقل اسم المستخدم
        var userGroup = BuildFieldGroup("اسم المستخدم", "👤", _usernameBox, "admin");
        userGroup.Top  = 0;
        userGroup.Left = 0;
        userGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // حقل كلمة المرور
        _passwordBox.UseSystemPasswordChar = true;
        var passGroup = BuildFieldGroup("كلمة المرور", "🔒", _passwordBox, "••••••••");
        passGroup.Top    = 88;
        passGroup.Left   = 0;
        passGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // CapsLock تحذير
        _capsLockLabel.Text        = "⚠️  CapsLock مفعّل";
        _capsLockLabel.Font        = new Font("Tahoma", 8.5F);
        _capsLockLabel.ForeColor   = Color.FromArgb(150, 100, 0);
        _capsLockLabel.BackColor   = Color.FromArgb(255, 248, 220);
        _capsLockLabel.TextAlign   = ContentAlignment.MiddleRight;
        _capsLockLabel.Padding     = new Padding(0, 0, 8, 0);
        _capsLockLabel.BorderStyle = BorderStyle.FixedSingle;
        _capsLockLabel.Visible     = false;
        _capsLockLabel.Size        = new Size(340, 26);
        _capsLockLabel.Top         = 176;
        _capsLockLabel.Left        = 0;
        _capsLockLabel.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // checkbox إظهار كلمة المرور
        _showPassCheck.Text      = "إظهار كلمة المرور";
        _showPassCheck.Font      = new Font("Tahoma", 9F);
        _showPassCheck.ForeColor = TextMuted;
        _showPassCheck.BackColor = Color.Transparent;
        _showPassCheck.RightToLeft = RightToLeft.Yes;
        _showPassCheck.Size      = new Size(180, 24);
        _showPassCheck.Top       = 208;
        _showPassCheck.Left      = 0;
        _showPassCheck.Anchor    = AnchorStyles.Top | AnchorStyles.Right;

        fieldsPanel.Controls.Add(userGroup);
        fieldsPanel.Controls.Add(passGroup);
        fieldsPanel.Controls.Add(_capsLockLabel);
        fieldsPanel.Controls.Add(_showPassCheck);

        // ── رسالة خطأ ─────────────────────────────────────────
        _errorLabel.Dock        = DockStyle.Top;
        _errorLabel.Height      = 0;
        _errorLabel.Font        = new Font("Tahoma", 9.5F);
        _errorLabel.ForeColor   = Color.White;
        _errorLabel.BackColor   = Color.FromArgb(197, 48, 48);
        _errorLabel.TextAlign   = ContentAlignment.MiddleCenter;
        _errorLabel.Visible     = false;
        _errorLabel.Padding     = new Padding(8);

        // ── زر الدخول ─────────────────────────────────────────
        StyleLoginButton();
        var btnWrap = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 58,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 8, 0, 0)
        };
        _loginButton.Dock = DockStyle.Fill;
        btnWrap.Controls.Add(_loginButton);

        // ── محاولات / مساعدة ──────────────────────────────────
        _attemptsLabel.Dock      = DockStyle.Top;
        _attemptsLabel.Height    = 28;
        _attemptsLabel.Font      = new Font("Tahoma", 8.5F);
        _attemptsLabel.ForeColor = TextMuted;
        _attemptsLabel.TextAlign = ContentAlignment.MiddleCenter;
        _attemptsLabel.Text      = "للمساعدة تواصل مع مدير النظام";
        _attemptsLabel.BackColor = Color.Transparent;

        // ── تجميع ─────────────────────────────────────────────
        var contentFlow = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 10, 0, 0)
        };

        contentFlow.Controls.Add(_attemptsLabel);
        contentFlow.Controls.Add(btnWrap);
        contentFlow.Controls.Add(_errorLabel);
        contentFlow.Controls.Add(fieldsPanel);

        card.Controls.Add(contentFlow);
        card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent });
        card.Controls.Add(divider);
        card.Controls.Add(titleWrap);

        // تعديل عرض حقول بشكل Responsive
        card.Layout += (_, _) =>
        {
            int w = card.Width - 80; // Padding 40 يمين + يسار
            userGroup.Width  = w;
            passGroup.Width  = w;
            _capsLockLabel.Width = w;
        };

        return card;
    }

    private Panel BuildFieldGroup(string label, string icon, TextBox tb, string placeholder)
    {
        var group = new Panel
        {
            Height    = 80,
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text      = $"{icon}  {label}",
            Font      = new Font("Tahoma", 9.5F, FontStyle.Bold),
            ForeColor = TextDark,
            Size      = new Size(200, 24),
            Location  = new Point(group.Width - 200, 0),
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight
        };

        tb.Font        = new Font("Tahoma", 11F);
        tb.BackColor   = Color.White;
        tb.ForeColor   = TextDark;
        tb.BorderStyle = BorderStyle.FixedSingle;
        tb.PlaceholderText = placeholder;
        tb.Size        = new Size(group.Width, 44);
        tb.Location    = new Point(0, 32);
        tb.Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // focus glow
        tb.Enter += (_, _) =>
        {
            tb.BackColor = Color.FromArgb(240, 248, 255);
            group.Invalidate();
        };
        tb.Leave += (_, _) =>
        {
            tb.BackColor = Color.White;
            group.Invalidate();
        };

        // رسم border زرقاء عند التركيز
        group.Paint += (_, e) =>
        {
            bool focused = tb.Focused;
            var rect = new Rectangle(0, 32, group.Width - 1, 43);
            using var pen = new Pen(focused
                ? Color.FromArgb(46, 134, 193)
                : Color.FromArgb(200, 210, 225), focused ? 2 : 1);
            e.Graphics.DrawRectangle(pen, rect);
        };

        group.Layout += (_, _) =>
        {
            tb.Width    = group.Width;
            lbl.Left    = group.Width - lbl.Width;
        };

        group.Controls.Add(tb);
        group.Controls.Add(lbl);
        return group;
    }

    private void StyleLoginButton()
    {
        _loginButton.Text      = "تسجيل الدخول  →";
        _loginButton.Font      = new Font("Tahoma", 12F, FontStyle.Bold);
        _loginButton.BackColor = BtnPrimary;
        _loginButton.ForeColor = Color.White;
        _loginButton.FlatStyle = FlatStyle.Flat;
        _loginButton.FlatAppearance.BorderSize  = 0;
        _loginButton.FlatAppearance.MouseOverBackColor = BtnHover;
        _loginButton.Cursor    = Cursors.Hand;
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
        _loginButton.Text    = loading ? "⏳  جاري التحقق..." : "تسجيل الدخول  →";
        _loginButton.BackColor = BtnPrimary;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _lockTimer?.Stop();
        _lockTimer?.Dispose();
        base.OnFormClosed(e);
    }
}
