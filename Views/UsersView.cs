using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-005 — شاشة إدارة المستخدمين (Admin فقط)
/// </summary>
internal sealed class UsersView : UserControl
{
    private readonly DataGridView _grid;
    private readonly Button       _addButton;
    private readonly Button       _editButton;
    private readonly Button       _toggleButton;
    private readonly Button       _changePassButton;
    private readonly TextBox      _searchBox;
    private readonly Label        _feedbackLabel;
    private List<User>            _allUsers = new();

    public UsersView()
    {
        _grid             = new DataGridView();
        _addButton        = new Button { Text = "إضافة مستخدم" };
        _editButton       = new Button { Text = "تعديل" };
        _toggleButton     = new Button { Text = "تفعيل / تعطيل" };
        _changePassButton = new Button { Text = "تغيير كلمة المرور" };
        _searchBox        = AppTheme.CreateTextBox("بحث بالاسم أو اسم المستخدم...");
        _feedbackLabel    = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        BuildUI();
        WireEvents();
        LoadUsers();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock       = DockStyle.Fill,
            ColumnCount = 1,
            RowCount   = 3,
            Padding    = new Padding(8),
            BackColor  = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(),    0, 1);
        root.Controls.Add(BuildFooter(),  0, 2);

        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            BackColor   = AppTheme.Surface
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Search
        _searchBox.Dock = DockStyle.Fill;

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false
        };

        AppTheme.StylePrimaryButton(_addButton);
        AppTheme.StyleSecondaryButton(_editButton);
        AppTheme.StyleSecondaryButton(_toggleButton);
        AppTheme.StyleSecondaryButton(_changePassButton);

        _addButton.Width        = 150;
        _editButton.Width       = 110;
        _toggleButton.Width     = 140;
        _changePassButton.Width = 170;

        foreach (var btn in new[]{_addButton, _editButton, _toggleButton, _changePassButton})
            btn.Margin = new Padding(4, 0, 4, 0);

        btnPanel.Controls.Add(_addButton);
        btnPanel.Controls.Add(_editButton);
        btnPanel.Controls.Add(_toggleButton);
        btnPanel.Controls.Add(_changePassButton);

        bar.Controls.Add(_searchBox, 0, 0);
        bar.Controls.Add(btnPanel,   1, 0);

        return bar;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(4);

        _grid.Dock                  = DockStyle.Fill;
        _grid.ReadOnly              = true;
        _grid.AllowUserToAddRows    = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect           = false;
        _grid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible     = false;
        _grid.BorderStyle           = BorderStyle.None;
        _grid.Font                  = AppTheme.BodyFont;
        _grid.BackgroundColor       = AppTheme.Surface;
        _grid.GridColor             = AppTheme.Border;

        _grid.ColumnHeadersDefaultCellStyle.BackColor   = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor   = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font        = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleCenter;
        _grid.EnableHeadersVisualStyles                 = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "#",              Width = 50,  FillWeight = 5  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUsername",  HeaderText = "اسم المستخدم",   FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFullName",  HeaderText = "الاسم الكامل",   FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRole",      HeaderText = "الدور",          FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "الحالة",         FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastLogin", HeaderText = "آخر دخول",       FillWeight = 20 });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text      = "اختر مستخدمًا من القائمة للتعديل أو تغيير كلمة المرور.";
        panel.Controls.Add(_feedbackLabel);
        return panel;
    }

    private void WireEvents()
    {
        _searchBox.TextChanged    += (_, _) => FilterGrid(_searchBox.Text);
        _addButton.Click          += (_, _) => OpenUserDialog(null);
        _editButton.Click         += (_, _) => EditSelected();
        _toggleButton.Click       += (_, _) => ToggleSelected();
        _changePassButton.Click   += (_, _) => ChangePasswordSelected();
        _grid.CellDoubleClick     += (_, _) => EditSelected();
    }

    private void LoadUsers()
    {
        try
        {
            var repo = new UserRepository();
            _allUsers = repo.GetAll();
            PopulateGrid(_allUsers);
            ShowInfo($"تم تحميل {_allUsers.Count} مستخدم.");
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل المستخدمين: {ex.Message}");
        }
    }

    private void PopulateGrid(List<User> users)
    {
        _grid.Rows.Clear();
        foreach (var u in users)
        {
            int rowIdx = _grid.Rows.Add(
                u.Id,
                u.Username,
                u.FullName,
                u.RoleNameAr,
                u.IsActive ? "✅ نشط" : "❌ معطّل",
                u.LastLogin.HasValue ? u.LastLogin.Value.ToString("dd/MM/yyyy HH:mm") : "—"
            );

            if (!u.IsActive)
                _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = AppTheme.MutedText;
        }
    }

    private void FilterGrid(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            PopulateGrid(_allUsers);
            return;
        }

        var filtered = _allUsers.Where(u =>
            u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            u.Username.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        PopulateGrid(filtered);
    }

    private User? GetSelectedUser()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _allUsers.FirstOrDefault(u => u.Id == id);
    }

    private void EditSelected()
    {
        var user = GetSelectedUser();
        if (user is null) { ShowError("اختر مستخدمًا أولاً."); return; }
        OpenUserDialog(user);
    }

    private void ToggleSelected()
    {
        var user = GetSelectedUser();
        if (user is null) { ShowError("اختر مستخدمًا أولاً."); return; }

        if (user.Id == SessionContext.CurrentUser?.Id)
        {
            ShowError("لا يمكنك تعطيل حسابك الخاص.");
            return;
        }

        string action  = user.IsActive ? "تعطيل" : "تفعيل";
        var    confirm = MessageBox.Show(
            $"هل تريد {action} المستخدم [{user.FullName}]؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            user.IsActive = !user.IsActive;
            new UserRepository().Update(user);
            AuditService.LogUpdate("auth.users", user.Id);
            LoadUsers();
            ShowSuccess($"تم {action} المستخدم بنجاح.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ChangePasswordSelected()
    {
        var user = GetSelectedUser();
        if (user is null) { ShowError("اختر مستخدمًا أولاً."); return; }

        using var dlg = new ChangePasswordDialog(user);
        if (dlg.ShowDialog() == DialogResult.OK)
            ShowSuccess("تم تغيير كلمة المرور بنجاح.");
    }

    private void OpenUserDialog(User? user)
    {
        using var dlg = new UserDialog(user);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadUsers();
            ShowSuccess(user is null ? "تمت إضافة المستخدم بنجاح." : "تم تحديث بيانات المستخدم.");
        }
    }

    private void ShowSuccess(string msg) { _feedbackLabel.ForeColor = AppTheme.Success; _feedbackLabel.Text = msg; }
    private void ShowError(string msg)   { _feedbackLabel.ForeColor = AppTheme.Danger;  _feedbackLabel.Text = msg; }
    private void ShowInfo(string msg)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = msg; }
}
