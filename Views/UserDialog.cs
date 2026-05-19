using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// Dialog إضافة أو تعديل مستخدم
/// </summary>
internal sealed class UserDialog : Form
{
    private readonly User?   _existingUser;
    private readonly TextBox _usernameBox;
    private readonly TextBox _fullNameBox;
    private readonly TextBox _passwordBox;
    private readonly TextBox _confirmBox;
    private readonly ComboBox _roleCombo;
    private readonly CheckBox _activeCheck;
    private readonly Label   _errorLabel;

    // أدوار النظام — يجب أن تتطابق مع auth.roles في DB
    private static readonly (int Id, string NameAr)[] Roles =
    {
        (1, "مدير النظام"),
        (2, "كاشير"),
        (3, "أمين المخزن"),
        (4, "محاسب")
    };

    public UserDialog(User? existingUser)
    {
        _existingUser = existingUser;
        _usernameBox  = AppTheme.CreateTextBox("مثال: ahmed.ali");
        _fullNameBox  = AppTheme.CreateTextBox("الاسم الكامل");
        _passwordBox  = AppTheme.CreateTextBox("كلمة المرور");
        _confirmBox   = AppTheme.CreateTextBox("تأكيد كلمة المرور");
        _passwordBox.UseSystemPasswordChar = true;
        _confirmBox.UseSystemPasswordChar  = true;
        _roleCombo    = AppTheme.CreateComboBox();
        _activeCheck  = new CheckBox { Text = "نشط", Checked = true, Font = AppTheme.BodyFont };
        _errorLabel   = new Label();

        Text            = existingUser is null ? "إضافة مستخدم جديد" : $"تعديل: {existingUser.FullName}";
        Size            = new Size(440, 480);
        MinimumSize     = new Size(440, 480);
        MaximumSize     = new Size(440, 480);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        BackColor       = AppTheme.Surface;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        BuildUI();
        PopulateRoles();

        if (existingUser is not null)
            FillExistingData(existingUser);
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock       = DockStyle.Fill,
            ColumnCount = 1,
            Padding    = new Padding(24, 16, 24, 16),
            BackColor  = AppTheme.Surface
        };

        void AddRow(string label, Control ctrl)
        {
            layout.Controls.Add(AppTheme.CreateFieldLabel(label));
            ctrl.Dock = DockStyle.Top;
            layout.Controls.Add(ctrl);
        }

        AddRow("اسم المستخدم (Login)", _usernameBox);
        AddRow("الاسم الكامل", _fullNameBox);

        // في حالة تعديل، كلمة المرور اختيارية
        string passLabel = _existingUser is null ? "كلمة المرور" : "كلمة المرور الجديدة (اتركها فارغة للإبقاء)";
        AddRow(passLabel, _passwordBox);
        AddRow("تأكيد كلمة المرور", _confirmBox);
        AddRow("الدور", _roleCombo);

        _activeCheck.Dock = DockStyle.Top;
        layout.Controls.Add(_activeCheck);

        // Error
        _errorLabel.Dock      = DockStyle.Top;
        _errorLabel.Height    = 36;
        _errorLabel.Font      = AppTheme.BodyFont;
        _errorLabel.ForeColor = AppTheme.Danger;
        _errorLabel.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_errorLabel);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height        = 54,
            Padding       = new Padding(0, 8, 0, 0)
        };

        var saveBtn   = new Button { Text = "حفظ", Width = 130 };
        var cancelBtn = new Button { Text = "إلغاء", Width = 110 };
        AppTheme.StylePrimaryButton(saveBtn);
        AppTheme.StyleSecondaryButton(cancelBtn);
        saveBtn.Click   += (_, _) => Save();
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(saveBtn);
        btnPanel.Controls.Add(cancelBtn);

        Controls.Add(layout);
        Controls.Add(btnPanel);
    }

    private void PopulateRoles()
    {
        _roleCombo.Items.Clear();
        foreach (var r in Roles)
            _roleCombo.Items.Add(r.NameAr);
        _roleCombo.SelectedIndex = 1; // كاشير افتراضي
    }

    private void FillExistingData(User u)
    {
        _usernameBox.Text = u.Username;
        _fullNameBox.Text = u.FullName;
        _activeCheck.Checked = u.IsActive;
        int idx = Array.FindIndex(Roles, r => r.Id == u.RoleId);
        if (idx >= 0) _roleCombo.SelectedIndex = idx;

        // اسم المستخدم غير قابل للتعديل في حالة edit
        _usernameBox.ReadOnly = true;
        _usernameBox.BackColor = AppTheme.Background;
    }

    private void Save()
    {
        var username = _usernameBox.Text.Trim();
        var fullName = _fullNameBox.Text.Trim();
        var password = _passwordBox.Text;
        var confirm  = _confirmBox.Text;
        int roleId   = Roles[_roleCombo.SelectedIndex].Id;

        // Validation
        if (string.IsNullOrWhiteSpace(username))   { ShowError("اسم المستخدم مطلوب."); return; }
        if (string.IsNullOrWhiteSpace(fullName))   { ShowError("الاسم الكامل مطلوب."); return; }
        if (_roleCombo.SelectedIndex < 0)          { ShowError("يجب اختيار الدور."); return; }

        // كلمة المرور مطلوبة عند الإضافة
        if (_existingUser is null && string.IsNullOrWhiteSpace(password))
        {
            ShowError("كلمة المرور مطلوبة للمستخدم الجديد."); return;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (password.Length < 6) { ShowError("كلمة المرور يجب أن تكون 6 أحرف على الأقل."); return; }
            if (password != confirm) { ShowError("كلمة المرور وتأكيدها غير متطابقتين."); return; }
        }

        try
        {
            var repo = new UserRepository();
            var user = new User
            {
                Username = username,
                FullName = fullName,
                RoleId   = roleId,
                IsActive = _activeCheck.Checked
            };

            if (_existingUser is null)
            {
                // إضافة جديد
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                int newId = repo.Insert(user, hash);
                AuditService.LogCreate("auth.users", newId);
            }
            else
            {
                // تعديل
                user.Id = _existingUser.Id;
                repo.Update(user);

                if (!string.IsNullOrWhiteSpace(password))
                {
                    string hash = BCrypt.Net.BCrypt.HashPassword(password);
                    repo.UpdatePassword(user.Id, hash);
                }

                AuditService.LogUpdate("auth.users", user.Id);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ: {ex.Message}");
        }
    }

    private void ShowError(string msg) => _errorLabel.Text = msg;
}

// ─────────────────────────────────────────────────────────────────
/// <summary>Dialog تغيير كلمة مرور مستخدم محدد</summary>
internal sealed class ChangePasswordDialog : Form
{
    private readonly User    _user;
    private readonly TextBox _newPassBox;
    private readonly TextBox _confirmBox;
    private readonly Label   _errorLabel;

    public ChangePasswordDialog(User user)
    {
        _user       = user;
        _newPassBox = AppTheme.CreateTextBox("كلمة المرور الجديدة");
        _confirmBox = AppTheme.CreateTextBox("تأكيد كلمة المرور");
        _newPassBox.UseSystemPasswordChar = true;
        _confirmBox.UseSystemPasswordChar  = true;
        _errorLabel = new Label();

        Text            = $"تغيير كلمة مرور — {user.FullName}";
        Size            = new Size(400, 300);
        MinimumSize     = new Size(400, 300);
        MaximumSize     = new Size(400, 300);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        BackColor       = AppTheme.Surface;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        BuildUI();
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock       = DockStyle.Fill,
            ColumnCount = 1,
            Padding    = new Padding(24, 20, 24, 16),
            BackColor  = AppTheme.Surface
        };

        layout.Controls.Add(AppTheme.CreateFieldLabel($"المستخدم: {_user.FullName}"));
        layout.Controls.Add(AppTheme.CreateFieldLabel("كلمة المرور الجديدة"));
        _newPassBox.Dock = DockStyle.Top; layout.Controls.Add(_newPassBox);
        layout.Controls.Add(AppTheme.CreateFieldLabel("تأكيد كلمة المرور"));
        _confirmBox.Dock = DockStyle.Top; layout.Controls.Add(_confirmBox);

        _errorLabel.Dock      = DockStyle.Top;
        _errorLabel.Height    = 32;
        _errorLabel.Font      = AppTheme.BodyFont;
        _errorLabel.ForeColor = AppTheme.Danger;
        _errorLabel.TextAlign = ContentAlignment.MiddleCenter;
        layout.Controls.Add(_errorLabel);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 54,
            Padding = new Padding(0, 8, 0, 0)
        };

        var saveBtn   = new Button { Text = "حفظ",  Width = 130 };
        var cancelBtn = new Button { Text = "إلغاء", Width = 110 };
        AppTheme.StylePrimaryButton(saveBtn);
        AppTheme.StyleSecondaryButton(cancelBtn);
        saveBtn.Click   += (_, _) => Save();
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(saveBtn);
        btnPanel.Controls.Add(cancelBtn);

        Controls.Add(layout);
        Controls.Add(btnPanel);
    }

    private void Save()
    {
        var pass    = _newPassBox.Text;
        var confirm = _confirmBox.Text;
        if (pass.Length < 6) { _errorLabel.Text = "كلمة المرور يجب أن تكون 6 أحرف على الأقل."; return; }
        if (pass != confirm) { _errorLabel.Text = "كلمتا المرور غير متطابقتين."; return; }

        try
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(pass);
            new UserRepository().UpdatePassword(_user.Id, hash);
            AuditService.LogUpdate("auth.users", _user.Id);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { _errorLabel.Text = $"خطأ: {ex.Message}"; }
    }
}
