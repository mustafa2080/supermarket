using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-011 — شاشة قائمة الموردين: بحث، إضافة، تعديل، تفعيل/تعطيل، كشف حساب
/// </summary>
internal sealed class SuppliersView : UserControl
{
    private readonly DataGridView _grid;
    private readonly TextBox      _searchBox;
    private readonly CheckBox     _showAllCheck;
    private readonly Button       _addBtn;
    private readonly Button       _editBtn;
    private readonly Button       _toggleBtn;
    private readonly Button       _statementBtn;
    private readonly Label        _feedbackLabel;
    private readonly Label        _countLabel;
    private List<Supplier>        _all = new();

    public SuppliersView()
    {
        _grid         = new DataGridView();
        _searchBox    = AppTheme.CreateTextBox("بحث بالاسم أو الهاتف أو الكود...");
        _showAllCheck = new CheckBox
        {
            Text      = "عرض غير النشطين",
            Font      = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText,
            AutoSize  = true,
            Checked   = false
        };
        _addBtn       = new Button { Text = "إضافة مورد" };
        _editBtn      = new Button { Text = "تعديل" };
        _toggleBtn    = new Button { Text = "تفعيل / تعطيل" };
        _statementBtn = new Button { Text = "📄 كشف حساب" };
        _feedbackLabel = new Label();
        _countLabel    = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        BuildUI();
        WireEvents();
        LoadData();
    }

    // ── بناء الواجهة ─────────────────────────────────────────
    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(8), BackColor = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(),    0, 1);
        root.Controls.Add(BuildFooter(),  0, 2);
        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, BackColor = AppTheme.Surface
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _searchBox.Dock   = DockStyle.Fill;
        _showAllCheck.Dock = DockStyle.Fill;

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        AppTheme.StylePrimaryButton(_addBtn);
        AppTheme.StyleSecondaryButton(_editBtn);
        AppTheme.StyleSecondaryButton(_toggleBtn);
        AppTheme.StyleSecondaryButton(_statementBtn);

        foreach (var b in new[] { _addBtn, _editBtn, _toggleBtn, _statementBtn })
        {
            b.Width  = 140;
            b.Margin = new Padding(4, 0, 4, 0);
            btnPanel.Controls.Add(b);
        }

        bar.Controls.Add(_searchBox,    0, 0);
        bar.Controls.Add(_showAllCheck, 1, 0);
        bar.Controls.Add(btnPanel,      2, 0);
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
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _grid.EnableHeadersVisualStyles               = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",      HeaderText = "#",             Width = 50,  FillWeight = 4  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",    HeaderText = "الكود",         FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",    HeaderText = "اسم المورد",    FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPhone",   HeaderText = "التليفون",      FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMobile",  HeaderText = "الموبايل",      FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail",   HeaderText = "البريد",        FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCredit",  HeaderText = "حد الائتمان",   FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBalance", HeaderText = "المستحق",       FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "الحالة",        FillWeight = 6  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.Surface
        };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text      = "انقر نقراً مزدوجاً على مورد للتعديل — انقر 'كشف حساب' لعرض المعاملات.";

        _countLabel.Font      = AppTheme.SmallFont;
        _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft;
        _countLabel.AutoSize  = true;

        p.Controls.Add(_feedbackLabel, 0, 0);
        p.Controls.Add(_countLabel,    1, 0);
        return p;
    }

    // ── الأحداث ──────────────────────────────────────────────
    private void WireEvents()
    {
        _searchBox.TextChanged          += (_, _) => ApplyFilter();
        _showAllCheck.CheckedChanged    += (_, _) => LoadData();
        _addBtn.Click                   += (_, _) => OpenDialog(null);
        _editBtn.Click                  += (_, _) => EditSelected();
        _toggleBtn.Click                += (_, _) => ToggleSelected();
        _statementBtn.Click             += (_, _) => ShowStatement();
        _grid.CellDoubleClick           += (_, _) => EditSelected();
    }

    // ── تحميل البيانات ───────────────────────────────────────
    private void LoadData()
    {
        try
        {
            var repo = new SupplierRepository();
            _all = repo.GetAll(activeOnly: !_showAllCheck.Checked);
            PopulateGrid(_all);
            ShowInfo($"تم تحميل {_all.Count} مورد.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid(List<Supplier> items)
    {
        _grid.Rows.Clear();
        var repo = new SupplierRepository();
        foreach (var s in items)
        {
            decimal balance = 0;
            try { balance = repo.GetBalance(s.Id); } catch { /* تجاهل */ }

            int idx = _grid.Rows.Add(
                s.Id,
                string.IsNullOrEmpty(s.Code) ? "—" : s.Code,
                s.Name,
                string.IsNullOrEmpty(s.Phone)  ? "—" : s.Phone,
                string.IsNullOrEmpty(s.Mobile) ? "—" : s.Mobile,
                string.IsNullOrEmpty(s.Email)  ? "—" : s.Email,
                s.CreditLimit > 0 ? s.CreditLimit.ToString("N2") : "—",
                balance > 0 ? balance.ToString("N2") : "صفر",
                s.IsActive ? "✅ نشط" : "❌ معطّل"
            );

            if (!s.IsActive)
                _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.MutedText;
            else if (balance > 0)
                _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.Warning;
        }
        _countLabel.Text = $"الإجمالي: {items.Count}";
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) { PopulateGrid(_all); return; }
        var filtered = _all.Where(s =>
            s.Name.Contains(q,   StringComparison.OrdinalIgnoreCase) ||
            s.Phone.Contains(q,  StringComparison.OrdinalIgnoreCase) ||
            s.Mobile.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            s.Code.Contains(q,   StringComparison.OrdinalIgnoreCase) ||
            s.Email.Contains(q,  StringComparison.OrdinalIgnoreCase)
        ).ToList();
        PopulateGrid(filtered);
    }

    private Supplier? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _all.FirstOrDefault(s => s.Id == id);
    }

    private void EditSelected()
    {
        var s = GetSelected();
        if (s is null) { ShowError("اختر مورداً أولاً."); return; }
        OpenDialog(s);
    }

    private void ToggleSelected()
    {
        var s = GetSelected();
        if (s is null) { ShowError("اختر مورداً أولاً."); return; }

        string action = s.IsActive ? "تعطيل" : "تفعيل";
        var confirm = MessageBox.Show(
            $"هل تريد {action} المورد [{s.Name}]؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            new SupplierRepository().SetActive(s.Id, !s.IsActive);
            AuditService.LogUpdate("public.suppliers", s.Id);
            LoadData();
            ShowSuccess($"تم {action} المورد بنجاح.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ShowStatement()
    {
        var s = GetSelected();
        if (s is null) { ShowError("اختر مورداً أولاً."); return; }

        using var dlg = new SupplierStatementDialog(s);
        dlg.ShowDialog();
    }

    private void OpenDialog(Supplier? supplier)
    {
        using var dlg = new SupplierDialog(supplier);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess(supplier is null ? "تمت إضافة المورد بنجاح." : "تم تحديث بيانات المورد.");
        }
    }

    private void ShowSuccess(string m) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = m; }
    private void ShowError(string m)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
    private void ShowInfo(string m)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = m; }
}
