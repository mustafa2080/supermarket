using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

// ══════════════════════════════════════════════════════════════
//  لوحة مجموعات الأصناف
// ══════════════════════════════════════════════════════════════
internal sealed class GroupsPanel : UserControl
{
    private readonly DataGridView _grid;
    private readonly TextBox      _searchBox;
    private readonly Button       _addBtn;
    private readonly Button       _editBtn;
    private readonly Button       _deleteBtn;
    private readonly Label        _feedbackLabel;
    private readonly Label        _countLabel;
    private List<ItemGroup>       _all = new();

    public GroupsPanel()
    {
        _grid          = new DataGridView();
        _searchBox     = AppTheme.CreateTextBox("بحث باسم المجموعة...");
        _addBtn        = new Button { Text = "إضافة مجموعة" };
        _editBtn       = new Button { Text = "تعديل" };
        _deleteBtn     = new Button { Text = "حذف" };
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
            Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.Surface
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _searchBox.Dock = DockStyle.Fill;

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        AppTheme.StylePrimaryButton(_addBtn);
        AppTheme.StyleSecondaryButton(_editBtn);
        AppTheme.StyleSecondaryButton(_deleteBtn);
        _deleteBtn.ForeColor = AppTheme.Danger;

        foreach (var b in new[] { _addBtn, _editBtn, _deleteBtn })
        {
            b.Width  = 120;
            b.Margin = new Padding(4, 0, 4, 0);
            btnPanel.Controls.Add(b);
        }

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
        _grid.EnableHeadersVisualStyles                 = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",    HeaderText = "#",              Width = 50,  FillWeight = 5  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNameAr",HeaderText = "اسم المجموعة",  FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNameEn",HeaderText = "الاسم بالإنجليزي", FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCount", HeaderText = "عدد الأصناف",   FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",HeaderText = "الحالة",         FillWeight = 10 });

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
        _feedbackLabel.Text      = "انقر نقراً مزدوجاً للتعديل السريع.";

        _countLabel.Font      = AppTheme.SmallFont;
        _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft;
        _countLabel.AutoSize  = true;

        p.Controls.Add(_feedbackLabel, 0, 0);
        p.Controls.Add(_countLabel,    1, 0);
        return p;
    }

    private void WireEvents()
    {
        _searchBox.TextChanged += (_, _) => ApplyFilter();
        _addBtn.Click          += (_, _) => OpenDialog(null);
        _editBtn.Click         += (_, _) => EditSelected();
        _deleteBtn.Click       += (_, _) => DeleteSelected();
        _grid.CellDoubleClick  += (_, _) => EditSelected();
    }

    private void LoadData()
    {
        try
        {
            _all = new CategoryRepository().GetAllGroups();
            PopulateGrid(_all);
            ShowInfo($"تم تحميل {_all.Count} مجموعة.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid(List<ItemGroup> items)
    {
        _grid.Rows.Clear();
        foreach (var g in items)
        {
            int idx = _grid.Rows.Add(
                g.Id, g.NameAr, g.Name,
                g.ItemCount > 0 ? g.ItemCount.ToString() : "—",
                g.IsActive ? "✅ نشط" : "❌ معطّل"
            );
            if (!g.IsActive)
                _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.MutedText;
        }
        _countLabel.Text = $"الإجمالي: {items.Count}";
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(g =>
                g.NameAr.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                g.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        PopulateGrid(filtered);
    }

    private ItemGroup? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _all.FirstOrDefault(g => g.Id == id);
    }

    private void EditSelected()
    {
        var g = GetSelected();
        if (g is null) { ShowError("اختر مجموعة أولاً."); return; }
        OpenDialog(g);
    }

    private void DeleteSelected()
    {
        var g = GetSelected();
        if (g is null) { ShowError("اختر مجموعة أولاً."); return; }

        if (g.ItemCount > 0)
        {
            ShowError($"لا يمكن الحذف — المجموعة تحتوي {g.ItemCount} صنف.");
            return;
        }

        var confirm = MessageBox.Show(
            $"هل تريد حذف المجموعة [{g.NameAr}] نهائياً؟",
            "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            var (ok, err) = new CategoryRepository().DeleteGroup(g.Id);
            if (!ok) { ShowError(err); return; }
            AuditService.LogDelete("public.item_groups", g.Id);
            LoadData();
            ShowSuccess("تم حذف المجموعة بنجاح.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OpenDialog(ItemGroup? group)
    {
        using var dlg = new GroupDialog(group);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess(group is null ? "تمت إضافة المجموعة بنجاح." : "تم تحديث المجموعة.");
        }
    }

    private void ShowSuccess(string m) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = m; }
    private void ShowError(string m)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
    private void ShowInfo(string m)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = m; }
}
