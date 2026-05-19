using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-009 — شاشة قائمة الأصناف: بحث، تصفية، إضافة، تعديل، تفعيل/تعطيل
/// </summary>
internal sealed class ItemsView : UserControl
{
    private readonly DataGridView _grid;
    private readonly Button       _addButton;
    private readonly Button       _editButton;
    private readonly Button       _toggleButton;
    private readonly TextBox      _searchBox;
    private readonly ComboBox     _groupFilter;
    private readonly Label        _feedbackLabel;
    private readonly Label        _countLabel;
    private List<Item>            _allItems  = new();
    private List<ItemGroup>       _groups    = new();

    public ItemsView()
    {
        _grid          = new DataGridView();
        _addButton     = new Button { Text = "إضافة صنف" };
        _editButton    = new Button { Text = "تعديل" };
        _toggleButton  = new Button { Text = "تفعيل / تعطيل" };
        _searchBox     = AppTheme.CreateTextBox("بحث بالاسم أو الباركود أو الكود...");
        _groupFilter   = AppTheme.CreateComboBox();
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
            Dock        = DockStyle.Fill,
            ColumnCount  = 1,
            RowCount     = 3,
            Padding      = new Padding(8),
            BackColor    = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

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
            ColumnCount  = 3,
            BackColor    = AppTheme.Surface
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _searchBox.Dock  = DockStyle.Fill;
        _groupFilter.Dock = DockStyle.Fill;

        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection  = FlowDirection.RightToLeft,
            WrapContents   = false
        };

        AppTheme.StylePrimaryButton(_addButton);
        AppTheme.StyleSecondaryButton(_editButton);
        AppTheme.StyleSecondaryButton(_toggleButton);

        _addButton.Width    = 130;
        _editButton.Width   = 100;
        _toggleButton.Width = 140;

        foreach (var btn in new[] { _addButton, _editButton, _toggleButton })
            btn.Margin = new Padding(4, 0, 4, 0);

        btnPanel.Controls.Add(_addButton);
        btnPanel.Controls.Add(_editButton);
        btnPanel.Controls.Add(_toggleButton);

        bar.Controls.Add(_searchBox,  0, 0);
        bar.Controls.Add(_groupFilter, 1, 0);
        bar.Controls.Add(btnPanel,     2, 0);
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "#",             Width = 50,  FillWeight = 4  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",     HeaderText = "الكود",         FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",     HeaderText = "اسم الصنف",     FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBarcode",  HeaderText = "الباركود",      FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGroup",    HeaderText = "المجموعة",      FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",     HeaderText = "الوحدة",        FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPurchase", HeaderText = "سعر الشراء",    FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRetail",   HeaderText = "سعر التجزئة",   FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock",    HeaderText = "الرصيد",        FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "الحالة",        FillWeight = 6  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var panel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount  = 2,
            BackColor    = AppTheme.Surface
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text      = "انقر نقراً مزدوجاً على صنف للتعديل.";

        _countLabel.Dock      = DockStyle.Fill;
        _countLabel.Font      = AppTheme.SmallFont;
        _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft;

        panel.Controls.Add(_feedbackLabel, 0, 0);
        panel.Controls.Add(_countLabel,    1, 0);
        return panel;
    }

    // ── الأحداث ──────────────────────────────────────────────
    private void WireEvents()
    {
        _searchBox.TextChanged   += (_, _) => ApplyFilter();
        _groupFilter.SelectedIndexChanged += (_, _) => ApplyFilter();
        _addButton.Click         += (_, _) => OpenItemCard(null);
        _editButton.Click        += (_, _) => EditSelected();
        _toggleButton.Click      += (_, _) => ToggleSelected();
        _grid.CellDoubleClick    += (_, _) => EditSelected();
    }

    // ── تحميل البيانات ───────────────────────────────────────
    private void LoadData()
    {
        try
        {
            var repo  = new ItemRepository();
            _groups   = repo.GetGroups();
            _allItems = repo.GetAll(activeOnly: false);

            // تحميل فلتر المجموعات
            _groupFilter.Items.Clear();
            _groupFilter.Items.Add(new ComboItem(0, "كل المجموعات"));
            foreach (var g in _groups)
                _groupFilter.Items.Add(new ComboItem(g.Id, g.NameAr));
            _groupFilter.SelectedIndex = 0;

            PopulateGrid(_allItems);
            ShowInfo($"تم تحميل {_allItems.Count} صنف.");
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل الأصناف: {ex.Message}");
        }
    }

    private void PopulateGrid(List<Item> items)
    {
        _grid.Rows.Clear();
        foreach (var item in items)
        {
            int idx = _grid.Rows.Add(
                item.Id,
                item.ItemCode,
                item.NameAr,
                item.Barcode,
                item.GroupName,
                item.UnitName,
                item.PurchasePrice.ToString("N2"),
                item.RetailPrice.ToString("N2"),
                item.CurrentStock.ToString("N2"),
                item.IsActive ? "✅ نشط" : "❌ معطّل"
            );
            if (!item.IsActive)
                _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.MutedText;
        }
        _countLabel.Text = $"إجمالي: {items.Count} صنف";
    }

    private void ApplyFilter()
    {
        var query   = _searchBox.Text.Trim();
        int groupId = 0;
        if (_groupFilter.SelectedItem is ComboItem ci) groupId = ci.Id;

        var filtered = _allItems.Where(i =>
            (string.IsNullOrEmpty(query)
                || i.NameAr.Contains(query, StringComparison.OrdinalIgnoreCase)
                || i.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)
                || i.ItemCode.Contains(query, StringComparison.OrdinalIgnoreCase))
            && (groupId == 0 || i.GroupId == groupId)
        ).ToList();

        PopulateGrid(filtered);
    }

    private Item? GetSelectedItem()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _allItems.FirstOrDefault(i => i.Id == id);
    }

    private void EditSelected()
    {
        var item = GetSelectedItem();
        if (item is null) { ShowError("اختر صنفاً أولاً."); return; }
        OpenItemCard(item);
    }

    private void ToggleSelected()
    {
        var item = GetSelectedItem();
        if (item is null) { ShowError("اختر صنفاً أولاً."); return; }

        string action  = item.IsActive ? "تعطيل" : "تفعيل";
        var    confirm = MessageBox.Show(
            $"هل تريد {action} الصنف [{item.NameAr}]؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            item.IsActive = !item.IsActive;
            new ItemRepository().Update(item);
            AuditService.LogUpdate("public.items", item.Id);
            LoadData();
            ShowSuccess($"تم {action} الصنف بنجاح.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OpenItemCard(Item? item)
    {
        using var dlg = new ItemCardDialog(item);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess(item is null ? "تمت إضافة الصنف بنجاح." : "تم تحديث بيانات الصنف.");
        }
    }

    private void ShowSuccess(string msg) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = msg; }
    private void ShowError(string msg)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = msg; }
    private void ShowInfo(string msg)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = msg; }

    // Helper لعناصر الـ ComboBox
    private record ComboItem(int Id, string Label) { public override string ToString() => Label; }
}
