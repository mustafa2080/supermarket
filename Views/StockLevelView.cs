using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-018 — لوحة المخزون الحالي مع التنبيهات</summary>
internal sealed class StockLevelView : UserControl
{
    private readonly DataGridView _grid;
    private readonly ComboBox     _warehouseCombo;
    private readonly TextBox      _searchBox;
    private readonly ComboBox     _groupCombo;
    private readonly ComboBox     _statusCombo;
    private readonly Button       _refreshBtn;
    private readonly Button       _openingBtn;
    private readonly Button       _warehousesBtn;
    private readonly Label        _alertLabel;
    private readonly Label        _totalLabel;
    private readonly Label        _countLabel;
    private readonly Label        _feedbackLabel;
    private List<Warehouse>       _warehouses = new();

    public StockLevelView()
    {
        _grid           = new DataGridView();
        _warehouseCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 180 };
        _searchBox      = AppTheme.CreateTextBox("بحث بالصنف أو الباركود...");
        _groupCombo     = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 150 };
        _statusCombo    = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont, Width = 110 };
        _refreshBtn     = new Button { Text = "🔄 تحديث" };
        _openingBtn     = new Button { Text = "📥 رصيد افتتاحي" };
        _warehousesBtn  = new Button { Text = "🏭 المستودعات" };
        _alertLabel     = new Label();
        _totalLabel     = new Label();
        _countLabel     = new Label();
        _feedbackLabel  = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        BuildUI();
        WireEvents();
        LoadWarehouses();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(8), BackColor = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        root.Controls.Add(BuildToolbar(),  0, 0);
        root.Controls.Add(BuildFilters(),  0, 1);
        root.Controls.Add(BuildGrid(),     0, 2);
        root.Controls.Add(BuildFooter(),   0, 3);
        Controls.Add(root);

        // alerts
        _alertLabel.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _alertLabel.ForeColor = AppTheme.Danger;
        _alertLabel.TextAlign = ContentAlignment.MiddleRight;
    }

    private Control BuildToolbar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = AppTheme.Surface, Padding = new Padding(0, 6, 0, 0)
        };

        var whLabel = new Label { Text = "المستودع:", AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.DarkText, TextAlign = ContentAlignment.MiddleLeft };

        AppTheme.StyleSecondaryButton(_refreshBtn);
        AppTheme.StyleSecondaryButton(_openingBtn);
        AppTheme.StyleSecondaryButton(_warehousesBtn);

        foreach (var b in new[] { _refreshBtn, _openingBtn, _warehousesBtn })
        { b.Height = 36; b.Margin = new Padding(4, 0, 4, 0); }

        _warehouseCombo.Height = 34;

        bar.Controls.Add(_refreshBtn);
        bar.Controls.Add(_openingBtn);
        bar.Controls.Add(_warehousesBtn);
        bar.Controls.Add(new Label { Width = 20, Height = 1 }); // spacer
        bar.Controls.Add(whLabel);
        bar.Controls.Add(_warehouseCombo);
        return bar;
    }

    private Control BuildFilters()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = AppTheme.Surface, Padding = new Padding(0, 2, 0, 0)
        };
        _searchBox.Width  = 260; _searchBox.Height = 30;
        _groupCombo.Height = _statusCombo.Height = 30;

        var grpLabel  = new Label { Text = "المجموعة:",  AutoSize = true, Font = AppTheme.SmallFont, ForeColor = AppTheme.MutedText, TextAlign = ContentAlignment.MiddleLeft };
        var statLabel = new Label { Text = "الحالة:",    AutoSize = true, Font = AppTheme.SmallFont, ForeColor = AppTheme.MutedText, TextAlign = ContentAlignment.MiddleLeft };

        _statusCombo.Items.AddRange(new object[] { "الكل", "كافي", "منخفض", "نفاد" });
        _statusCombo.SelectedIndex = 0;

        bar.Controls.Add(_searchBox);
        bar.Controls.Add(new Label { Width = 12, Height = 1 });
        bar.Controls.Add(grpLabel);
        bar.Controls.Add(_groupCombo);
        bar.Controls.Add(new Label { Width = 12, Height = 1 });
        bar.Controls.Add(statLabel);
        bar.Controls.Add(_statusCombo);
        bar.Controls.Add(_alertLabel);
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",   HeaderText = "الكود",         FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",   HeaderText = "الصنف",         FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGroup",  HeaderText = "المجموعة",       FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",   HeaderText = "الوحدة",         FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",    HeaderText = "الرصيد",         FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",HeaderText = "نقطة إعادة الطلب",FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colValue",  HeaderText = "قيمة المخزون",  FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "الحالة",         FillWeight = 8  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = AppTheme.Surface };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _feedbackLabel.Dock = DockStyle.Fill; _feedbackLabel.Font = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        _totalLabel.Font      = new Font("Tahoma", 9F, FontStyle.Bold);
        _totalLabel.ForeColor = AppTheme.Primary;
        _totalLabel.TextAlign = ContentAlignment.MiddleLeft;
        _totalLabel.AutoSize  = true; _totalLabel.Margin = new Padding(12, 0, 0, 0);

        _countLabel.Font = AppTheme.SmallFont; _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.AutoSize = true; _countLabel.TextAlign = ContentAlignment.MiddleLeft;
        _countLabel.Margin = new Padding(12, 0, 0, 0);

        p.Controls.Add(_feedbackLabel, 0, 0);
        p.Controls.Add(_totalLabel,    1, 0);
        p.Controls.Add(_countLabel,    2, 0);
        return p;
    }

    private void WireEvents()
    {
        _searchBox.TextChanged   += (_, _) => LoadStock();
        _groupCombo.SelectedIndexChanged  += (_, _) => LoadStock();
        _statusCombo.SelectedIndexChanged += (_, _) => LoadStock();
        _warehouseCombo.SelectedIndexChanged += (_, _) => { LoadGroupFilter(); LoadStock(); };
        _refreshBtn.Click     += (_, _) => LoadStock();
        _openingBtn.Click     += (_, _) => OpenOpeningStock();
        _warehousesBtn.Click  += (_, _) => ManageWarehouses();
    }

    private void LoadWarehouses()
    {
        _warehouses = new WarehouseRepository().GetAll().Where(w => w.IsActive).ToList();
        _warehouseCombo.Items.Clear();
        foreach (var w in _warehouses) _warehouseCombo.Items.Add(w.Name);
        if (_warehouseCombo.Items.Count > 0) _warehouseCombo.SelectedIndex = 0;
        else LoadStock();
    }

    private void LoadGroupFilter()
    {
        var sel = _groupCombo.SelectedItem?.ToString();
        _groupCombo.Items.Clear();
        _groupCombo.Items.Add("الكل");
        foreach (var g in new WarehouseRepository().GetGroupNames()) _groupCombo.Items.Add(g);
        _groupCombo.SelectedIndex = 0;
        if (sel != null)
        {
            int idx = _groupCombo.Items.IndexOf(sel);
            if (idx >= 0) _groupCombo.SelectedIndex = idx;
        }
    }

    private void LoadStock()
    {
        if (_warehouseCombo.SelectedIndex < 0) return;
        int whId = _warehouses[_warehouseCombo.SelectedIndex].Id;

        string? search = string.IsNullOrWhiteSpace(_searchBox.Text) ? null : _searchBox.Text.Trim();
        string? group  = _groupCombo.SelectedIndex > 0 ? _groupCombo.SelectedItem?.ToString() : null;
        string? status = _statusCombo.SelectedIndex > 0 ? _statusCombo.SelectedItem?.ToString() : null;

        try
        {
            var levels = new WarehouseRepository().GetStockLevels(whId, search, group, status);
            PopulateGrid(levels);

            var (low, oos) = new WarehouseRepository().GetAlertCounts(whId);
            _alertLabel.Text = (low + oos) > 0
                ? $"⚠️ منخفض: {low}  ❌ نافد: {oos}"
                : string.Empty;
            _feedbackLabel.ForeColor = AppTheme.MutedText;
            _feedbackLabel.Text      = $"تم تحميل {levels.Count} صنف.";
        }
        catch (Exception ex) { _feedbackLabel.ForeColor = AppTheme.Danger; _feedbackLabel.Text = ex.Message; }
    }

    private void PopulateGrid(List<StockLevel> items)
    {
        _grid.Rows.Clear();
        decimal totalValue = 0;
        foreach (var l in items)
        {
            int idx = _grid.Rows.Add(
                l.ItemCode, l.ItemName, l.GroupName, l.UnitName,
                l.Quantity.ToString("G"),
                l.ReorderPoint.ToString("G"),
                l.StockValue.ToString("N2"),
                $"{l.StockStatusIcon} {l.StockStatus}"
            );
            var row = _grid.Rows[idx];
            row.DefaultCellStyle.BackColor = l.StockStatus == "نفاد"    ? Color.FromArgb(255, 235, 235) :
                                             l.StockStatus == "منخفض"   ? Color.FromArgb(255, 250, 230) :
                                             AppTheme.Surface;
            totalValue += l.StockValue;
        }
        _countLabel.Text = $"الأصناف: {items.Count}";
        _totalLabel.Text = $"إجمالي قيمة المخزون: {totalValue:N2} ج";
    }

    private void OpenOpeningStock()
    {
        using var dlg = new OpeningStockDialog();
        dlg.ShowDialog();
        LoadStock();
    }

    private void ManageWarehouses()
    {
        using var form = new WarehousesManagerForm();
        form.ShowDialog();
        LoadWarehouses();
    }
}
