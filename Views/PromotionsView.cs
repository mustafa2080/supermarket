using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-016 — شاشة قائمة العروض والخصومات</summary>
internal sealed class PromotionsView : UserControl
{
    private readonly DataGridView _grid;
    private readonly TextBox      _searchBox;
    private readonly CheckBox     _showAllCheck;
    private readonly Button       _addBtn;
    private readonly Button       _editBtn;
    private readonly Button       _toggleBtn;
    private readonly Label        _feedbackLabel;
    private readonly Label        _countLabel;
    private List<Promotion>       _all = new();

    public PromotionsView()
    {
        _grid         = new DataGridView();
        _searchBox    = AppTheme.CreateTextBox("بحث باسم العرض...");
        _showAllCheck = new CheckBox
        {
            Text = "عرض المنتهية والمعطَّلة", Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText, AutoSize = true, Checked = false
        };
        _addBtn       = new Button { Text = "➕ إضافة عرض" };
        _editBtn      = new Button { Text = "✏️ تعديل" };
        _toggleBtn    = new Button { Text = "🔄 تفعيل / تعطيل" };
        _feedbackLabel = new Label();
        _countLabel    = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        BuildUI();
        WireEvents();
        LoadData();
    }

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
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _searchBox.Dock    = DockStyle.Fill;
        _showAllCheck.Dock = DockStyle.Fill;

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        AppTheme.StylePrimaryButton(_addBtn);
        AppTheme.StyleSecondaryButton(_editBtn);
        AppTheme.StyleSecondaryButton(_toggleBtn);
        foreach (var b in new[] { _addBtn, _editBtn, _toggleBtn })
        { b.Width = 150; b.Margin = new Padding(4, 0, 4, 0); btnPanel.Controls.Add(b); }

        bar.Controls.Add(_searchBox,    0, 0);
        bar.Controls.Add(_showAllCheck, 1, 0);
        bar.Controls.Add(btnPanel,      2, 0);
        return bar;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",     HeaderText = "#",           Width = 50,  FillWeight = 4  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",   HeaderText = "اسم العرض",   FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",   HeaderText = "النوع",        FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTarget", HeaderText = "ينطبق على",   FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSummary",HeaderText = "ملخص العرض",  FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStart",  HeaderText = "من",           FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEnd",    HeaderText = "إلى",          FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "الحالة",       FillWeight = 8  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.Surface };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _feedbackLabel.Dock = DockStyle.Fill; _feedbackLabel.Font = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text = "انقر نقراً مزدوجاً على عرض للتعديل.";
        _countLabel.Font = AppTheme.SmallFont; _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft; _countLabel.AutoSize = true;
        p.Controls.Add(_feedbackLabel, 0, 0);
        p.Controls.Add(_countLabel,    1, 0);
        return p;
    }

    private void WireEvents()
    {
        _searchBox.TextChanged       += (_, _) => ApplyFilter();
        _showAllCheck.CheckedChanged += (_, _) => LoadData();
        _addBtn.Click                += (_, _) => OpenDialog(null);
        _editBtn.Click               += (_, _) => EditSelected();
        _toggleBtn.Click             += (_, _) => ToggleSelected();
        _grid.CellDoubleClick        += (_, _) => EditSelected();
    }

    private void LoadData()
    {
        try
        {
            _all = new PromotionRepository().GetAll();
            if (!_showAllCheck.Checked)
                _all = _all.Where(p => p.IsActive && p.EndDate >= DateTime.Today).ToList();
            PopulateGrid(_all);
            ShowInfo($"تم تحميل {_all.Count} عرض.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid(List<Promotion> items)
    {
        _grid.Rows.Clear();
        foreach (var p in items)
        {
            string status = !p.IsActive              ? "❌ معطَّل"
                          : DateTime.Today > p.EndDate ? "⏰ منتهي"
                          : DateTime.Today < p.StartDate ? "🔜 قادم"
                          : "✅ نشط";

            string target = p.AppliesTo == "item"
                ? $"صنف: {p.ItemName}"
                : $"مجموعة: {p.GroupName}";

            int idx = _grid.Rows.Add(
                p.Id, p.Name, p.TypeAr, target, p.Summary,
                p.StartDate.ToString("dd/MM/yyyy"),
                p.EndDate.ToString("dd/MM/yyyy"),
                status
            );

            var row = _grid.Rows[idx];
            if (!p.IsActive || DateTime.Today > p.EndDate)
                row.DefaultCellStyle.ForeColor = AppTheme.MutedText;
            else if (status == "✅ نشط")
                row.DefaultCellStyle.ForeColor = AppTheme.Success;
        }
        _countLabel.Text = $"الإجمالي: {items.Count}";
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) { PopulateGrid(_all); return; }
        var filtered = _all.Where(p =>
            p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.ItemName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.GroupName.Contains(q, StringComparison.OrdinalIgnoreCase)
        ).ToList();
        PopulateGrid(filtered);
    }

    private Promotion? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _all.FirstOrDefault(p => p.Id == id);
    }

    private void EditSelected()
    {
        var p = GetSelected();
        if (p is null) { ShowError("اختر عرضاً أولاً."); return; }
        OpenDialog(p);
    }

    private void ToggleSelected()
    {
        var p = GetSelected();
        if (p is null) { ShowError("اختر عرضاً أولاً."); return; }
        string action = p.IsActive ? "تعطيل" : "تفعيل";
        var confirm = MessageBox.Show($"هل تريد {action} العرض [{p.Name}]؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;
        try
        {
            new PromotionRepository().SetActive(p.Id, !p.IsActive);
            AuditService.LogUpdate("public.promotions", p.Id);
            LoadData();
            ShowSuccess($"تم {action} العرض بنجاح.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OpenDialog(Promotion? promo)
    {
        using var dlg = new PromotionDialog(promo);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess(promo is null ? "تمت إضافة العرض بنجاح." : "تم تحديث العرض.");
        }
    }

    private void ShowSuccess(string m) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = m; }
    private void ShowError(string m)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
    private void ShowInfo(string m)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = m; }
}
