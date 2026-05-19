using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-004 — شاشة تسجيل الدخول
/// </summary>
internal sealed class LoginForm : Form
{
    private readonly TextBox _usernameBox;
    private readonly TextBox _passwordBox;
    private readonly Button  _loginButton;
    private readonly Label   _errorLabel;
    private readonly Label   _attemptsLabel;
    private int _failedAttempts = 0;

    public LoginForm()
    {
        Text            = "Smart Market ERP — تسجيل الدخول";
        Size            = new Size(480, 560);
        MinimumSize     = new Size(480, 560);
        MaximumSize     = new Size(480, 560);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = AppTheme.Background;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        _usernameBox   = AppTheme.CreateTextBox("اسم المستخدم");
        _passwordBox   = AppTheme.CreateTextBox("كلمة المرور");
        _passwordBox.UseSystemPasswordChar = true;
        _loginButton   = new Button { Text = "دخول" };
        _errorLabel    = new Label();
        _attemptsLabel = new Label();

        BuildUI();

        _loginButton.Click  += (_, _) => TryLogin();
        _passwordBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) TryLogin(); };
        _usernameBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) _passwordBox.Focus(); };
    }

    private void BuildUI()
    {
        // Card وسط الشاشة
        var card = new Panel
        {
            Width       = 380,
            Height      = 440,
            BackColor   = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
        };
        card.Location = new Point((ClientSize.Width - card.Width) / 2,
                                  (ClientSize.Height - card.Height) / 2);

        // Logo / Title area
        var logoPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 100,
            BackColor = AppTheme.Primary
        };

        var appTitle = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = "Smart Market ERP",
            Font      = AppTheme.TitleFont,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var appSubtitle = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Text      = "نظام إدارة السوبر ماركت الذكي",
            Font      = AppTheme.SmallFont,
            ForeColor = Color.WhiteSmoke,
            TextAlign = ContentAlignment.MiddleCenter
        };

        logoPanel.Controls.Add(appTitle);
        logoPanel.Controls.Add(appSubtitle);

        // Form fields
        var formPanel = new TableLayoutPanel
        {
            Dock       = DockStyle.Fill,
            ColumnCount = 1,
            Padding    = new Padding(28, 20, 28, 20),
            BackColor  = AppTheme.Surface
        };

        // Username
        var userLabel = AppTheme.CreateFieldLabel("اسم المستخدم");
        userLabel.Dock = DockStyle.Top;
        _usernameBox.Dock = DockStyle.Top;
        _usernameBox.Height = 36;

        // Password
        var passLabel = AppTheme.CreateFieldLabel("كلمة المرور");
        passLabel.Dock = DockStyle.Top;
        _passwordBox.Dock = DockStyle.Top;
        _passwordBox.Height = 36;

        // Login button
        AppTheme.StylePrimaryButton(_loginButton);
        _loginButton.Dock   = DockStyle.Top;
        _loginButton.Height = 44;
        _loginButton.Margin = new Padding(0, 16, 0, 0);

        // Error label
        _errorLabel.Dock      = DockStyle.Top;
        _errorLabel.Height    = 40;
        _errorLabel.Font      = AppTheme.BodyFont;
        _errorLabel.ForeColor = AppTheme.Danger;
        _errorLabel.TextAlign = ContentAlignment.MiddleCenter;
        _errorLabel.Visible   = false;

        // Attempts label
        _attemptsLabel.Dock      = DockStyle.Bottom;
        _attemptsLabel.Height    = 28;
        _attemptsLabel.Font      = AppTheme.SmallFont;
        _attemptsLabel.ForeColor = AppTheme.MutedText;
        _attemptsLabel.TextAlign = ContentAlignment.MiddleCenter;

        formPanel.Controls.Add(userLabel);
        formPanel.Controls.Add(_usernameBox);
        formPanel.Controls.Add(passLabel);
        formPanel.Controls.Add(_passwordBox);
        formPanel.Controls.Add(_loginButton);
        formPanel.Controls.Add(_errorLabel);

        card.Controls.Add(formPanel);
        card.Controls.Add(logoPanel);
        card.Controls.Add(_attemptsLabel);
        Controls.Add(card);
    }

    private void TryLogin()
    {
        var username = _usernameBox.Text.Trim();
        var password = _passwordBox.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("يرجى إدخال اسم المستخدم وكلمة المرور.");
            return;
        }

        _loginButton.Enabled = false;
        _loginButton.Text    = "جاري التحقق...";

        try
        {
            var repo = new UserRepository();
            var user = repo.GetByUsername(username);

            // المستخدم غير موجود
            if (user is null)
            {
                HandleFailedAttempt();
                return;
            }

            // الحساب مقفول
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now)
            {
                var remaining = (user.LockedUntil.Value - DateTime.Now).Minutes + 1;
                ShowError($"الحساب مقفول. حاول مرة أخرى بعد {remaining} دقيقة.");
                _loginButton.Enabled = true;
                _loginButton.Text    = "دخول";
                return;
            }

            // غير نشط
            if (!user.IsActive)
            {
                ShowError("هذا الحساب معطّل. تواصل مع مدير النظام.");
                _loginButton.Enabled = true;
                _loginButton.Text    = "دخول";
                return;
            }

            // التحقق من كلمة المرور بـ BCrypt
            bool passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!passwordOk)
            {
                repo.IncrementLoginAttempts(user.Id);
                HandleFailedAttempt();
                return;
            }

            // ✅ تسجيل دخول ناجح
            repo.UpdateLastLogin(user.Id);

            var permissions = repo.GetPermissions(user.RoleId);
            SessionContext.StartSession(user, permissions);
            AuditService.LogLogin(user.Id);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في الاتصال بقاعدة البيانات:\n{ex.Message}");
            _loginButton.Enabled = true;
            _loginButton.Text    = "دخول";
        }
    }

    private void HandleFailedAttempt()
    {
        _failedAttempts++;
        int remaining = 5 - _failedAttempts;

        if (_failedAttempts >= 5)
        {
            ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.\nتم قفل الحساب لمدة 15 دقيقة.");
            _loginButton.Enabled    = false;
            _attemptsLabel.Text     = "الحساب مقفول مؤقتاً.";
            _attemptsLabel.ForeColor = AppTheme.Danger;
        }
        else
        {
            ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
            _attemptsLabel.Text     = $"محاولات متبقية: {remaining}";
            _attemptsLabel.ForeColor = AppTheme.Warning;
            _loginButton.Enabled    = true;
            _loginButton.Text       = "دخول";
        }

        _passwordBox.Clear();
        _passwordBox.Focus();
    }

    private void ShowError(string msg)
    {
        _errorLabel.Text    = msg;
        _errorLabel.Visible = true;
    }
}
