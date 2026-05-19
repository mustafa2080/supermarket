using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-018 — نموذج إدخال الرصيد الافتتاحي للمستودع</summary>
internal sealed class OpeningStockDialog : Form
{
    private readonly DataGridView _grid;
    private readonly ComboBox     _warehouseCombo;
    private readonly Button       _saveBtn;
    private readonly Button       _cancelBtn;
    private readonly Label        _feedbackLabel;
    private readonly Label        _infoLabel;
    private List<Warehouse>       _warehouses = new();

    public OpeningStockDialog()
    {
        Text            = "إدخال الرصيد الافتتاحي";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(720, 560);
        MinimumSize     = new Size(640, 480);
        BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        _grid           = new DataGridView();
        _warehouseCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = AppTheme.BodyFont };
        _saveBtn        = new Button { Text = "💾 حفظ الأرصدة" };
        _cancelBtn      = new Button { Text = "إغلاق" };
        _feedbackLabel  = new Label();
        _infoLabel      = new Label();

        BuildUI();
        LoadWarehouses();
        _warehouseCombo.SelectedIndexChanged += (_, _) => LoadItems();
        _saveBtn.Click   += (_, _) => Save();
        _cancelBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        // صف المستودع
        var topRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        var lbl = new Label { Text = "المستودع:", AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.DarkText, TextAlign = ContentAlignment.MiddleLeft };
        _warehouseCombo.Dock = DockStyle.Fill;
        topRow.Controls.Add(lbl,             0, 0);
        topRow.Controls.Add(_warehouseCombo, 1, 0);

        _infoLabel.Dock      = DockStyle.Fill;
        _infoLabel.Font      = AppTheme.SmallFont;
        _infoLabel.ForeColor = AppTheme.MutedText;
        _infoLabel.Text      = "أدخل الكميات الافتتاحية. الأصفار تعني رصيد صفر.";
        _infoLabel.TextAlign = ContentAlignment.MiddleRight;

        // الجريد
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _grid.Dock                  = DockStyle.Fill;
        _grid.AllowUserToAddRows    = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible     = false;
        _grid.BorderStyle           = BorderStyle.None;
        _grid.Font                  = AppTheme.BodyFont;
        _grid.BackgroundColor       = AppTheme.Surface;
        _grid.GridColor             = AppTheme.Border;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9F, FontStyle.Bold);
        _grid.EnableHeadersVisualStyles               = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",   Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "الكود",       FillWeight = 15, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "الصنف",       FillWeight = 45, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "الوحدة",      FillWeight = 15, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",  HeaderText = "الرصيد الافتتاحي", FillWeight = 25 });
        card.Controls.Add(_grid);

        // أزرار
        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(_cancelBtn);
        _saveBtn.Width = 160; _cancelBtn.Width = 90;
        _feedbackLabel.Font = AppTheme.SmallFont; _feedbackLabel.ForeColor = AppTheme.Danger; _feedbackLabel.AutoSize = true; _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        btnRow.Controls.Add(_saveBtn);
        btnRow.Controls.Add(_cancelBtn);
        btnRow.Controls.Add(_feedbackLabel);

        root.Controls.Add(topRow, 0, 0);
        root.Controls.Add(_infoLabel, 0, 1);
        root.Controls.Add(card,   0, 2);
        root.Controls.Add(btnRow, 0, 3);
        Controls.Add(root);
    }

    private void LoadWarehouses()
    {
        _warehouses = new WarehouseRepository().GetAll().Where(w => w.IsActive).ToList();
        _warehouseCombo.Items.Clear();
        foreach (var w in _warehouses) _warehouseCombo.Items.Add(w.Name);
        if (_warehouseCombo.Items.Count > 0) _warehouseCombo.SelectedIndex = 0;
    }

    private void LoadItems()
    {
        if (_warehouseCombo.SelectedIndex < 0) return;
        int whId = _warehouses[_warehouseCombo.SelectedIndex].Id;
        var levels = new WarehouseRepository().GetStockLevels(whId);
        _grid.Rows.Clear();
        foreach (var l in levels)
            _grid.Rows.Add(l.ItemId, l.ItemCode, l.ItemName, l.UnitName, l.Quantity.ToString("G"));
    }

    private void Save()
    {
        if (_warehouseCombo.SelectedIndex < 0) { _feedbackLabel.Text = "اختر مستودعاً."; return; }
        int whId = _warehouses[_warehouseCombo.SelectedIndex].Id;
        var lines = new List<(int, decimal)>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            int id = (int)row.Cells["colId"].Value;
            if (!decimal.TryParse(row.Cells["colQty"].Value?.ToString(), out decimal qty) || qty < 0) qty = 0;
            lines.Add((id, qty));
        }
        try
        {
            new WarehouseRepository().SaveOpeningStock(whId, lines);
            _feedbackLabel.ForeColor = AppTheme.Success;
            _feedbackLabel.Text = $"تم حفظ أرصدة {lines.Count} صنف بنجاح.";
            LoadItems(); // تحديث الأرقام
        }
        catch (Exception ex) { _feedbackLabel.ForeColor = AppTheme.Danger; _feedbackLabel.Text = ex.Message; }
    }
}
