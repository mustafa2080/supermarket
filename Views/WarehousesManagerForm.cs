using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-018 — نموذج إدارة المستودعات (قائمة + CRUD)</summary>
internal sealed class WarehousesManagerForm : Form
{
    private readonly DataGridView _grid;
    private readonly Button       _addBtn;
    private readonly Button       _editBtn;
    private readonly Label        _feedbackLabel;
    private List<Warehouse>       _all = new();

    public WarehousesManagerForm()
    {
        Text            = "إدارة المستودعات";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(640, 460);
        MinimumSize     = new Size(560, 380);
        BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        _grid          = new DataGridView();
        _addBtn        = new Button { Text = "➕ مستودع جديد" };
        _editBtn       = new Button { Text = "✏️ تعديل" };
        _feedbackLabel = new Label();

        BuildUI();
        WireEvents();
        LoadData();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        // toolbar
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        AppTheme.StylePrimaryButton(_addBtn);
        AppTheme.StyleSecondaryButton(_editBtn);
        _addBtn.Width = 160; _editBtn.Width = 100;
        _addBtn.Margin = _editBtn.Margin = new Padding(4, 0, 4, 0);
        bar.Controls.Add(_addBtn);
        bar.Controls.Add(_editBtn);

        // grid
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false; _grid.BorderStyle = BorderStyle.None;
        _grid.Font = AppTheme.BodyFont; _grid.BackgroundColor = AppTheme.Surface; _grid.GridColor = AppTheme.Border;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _grid.EnableHeadersVisualStyles               = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",       HeaderText = "#",          Width = 50, FillWeight = 5  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",     HeaderText = "الاسم",      FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLocation", HeaderText = "الموقع",     FillWeight = 35 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDefault",  HeaderText = "افتراضي",    FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",   HeaderText = "الحالة",     FillWeight = 12 });

        card.Controls.Add(_grid);

        // footer
        _feedbackLabel.Dock = DockStyle.Fill; _feedbackLabel.Font = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        root.Controls.Add(bar,            0, 0);
        root.Controls.Add(card,           0, 1);
        root.Controls.Add(_feedbackLabel, 0, 2);
        Controls.Add(root);
    }

    private void WireEvents()
    {
        _addBtn.Click         += (_, _) => OpenDialog(null);
        _editBtn.Click        += (_, _) => EditSelected();
        _grid.CellDoubleClick += (_, _) => EditSelected();
    }

    private void LoadData()
    {
        try
        {
            _all = new WarehouseRepository().GetAll();
            _grid.Rows.Clear();
            foreach (var w in _all)
                _grid.Rows.Add(w.Id, w.Name, w.Location,
                    w.IsDefault ? "⭐ نعم" : "",
                    w.IsActive  ? "✅ نشط" : "❌ متوقف");
            _feedbackLabel.Text = $"المستودعات: {_all.Count}";
        }
        catch (Exception ex) { _feedbackLabel.ForeColor = AppTheme.Danger; _feedbackLabel.Text = ex.Message; }
    }

    private Warehouse? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        return _all.FirstOrDefault(w => w.Id == id);
    }

    private void EditSelected()
    {
        var w = GetSelected();
        if (w is null) { _feedbackLabel.ForeColor = AppTheme.Danger; _feedbackLabel.Text = "اختر مستودعاً أولاً."; return; }
        OpenDialog(w);
    }

    private void OpenDialog(Warehouse? w)
    {
        using var dlg = new WarehouseDialog(w);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            AuditService.LogUpdate("public.warehouses", w?.Id ?? 0);
            LoadData();
            _feedbackLabel.ForeColor = AppTheme.Success;
            _feedbackLabel.Text = w is null ? "تم إضافة المستودع." : "تم تحديث المستودع.";
        }
    }
}
