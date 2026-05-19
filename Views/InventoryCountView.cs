using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة إدارة جلسات الجرد — TASK-019</summary>
internal class InventoryCountView : UserControl
{
    private readonly WarehouseRepository _repo = new();

    // ── عناصر ────────────────────────────────────────────────
    private Label    _lblTitle   = null!;
    private ComboBox _cmbWH      = null!;
    private Label    _lblWH      = null!;
    private Button   _btnNew     = null!;
    private Button   _btnRefresh = null!;
    private DataGridView _grid   = null!;
    private Button   _btnOpen    = null!;
    private Button   _btnReport  = null!;

    private List<Warehouse>      _warehouses = new();
    private List<InventoryCount> _counts     = new();

    public InventoryCountView()
    {
        InitializeComponent();
        LoadWarehouses();
    }

    private void InitializeComponent()
    {
        Dock            = DockStyle.Fill;
        BackColor       = AppTheme.Background;
        RightToLeft     = RightToLeft.Yes;
                Font            = AppTheme.BodyFont;

        _lblTitle = new Label
        {
            Text      = "📦 جرد المخزون",
            Font      = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary,
            AutoSize  = true,
            Location  = new Point(20, 20)
        };

        _lblWH = new Label { Text = "المستودع:", AutoSize = true, Location = new Point(20, 72), ForeColor = AppTheme.DarkText };
        _cmbWH = new ComboBox
        {
            Location      = new Point(90, 68),
            Width         = 220,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = AppTheme.Surface,
            ForeColor     = AppTheme.DarkText
        };
        _cmbWH.SelectedIndexChanged += (_, _) => LoadCounts();

        _btnNew = new Button
        {
            Text      = "➕ جرد جديد",
            Location  = new Point(330, 64),
            Size      = new Size(130, 34),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnNew.FlatAppearance.BorderSize = 0;
        _btnNew.Click += OnNew;

        _btnRefresh = new Button
        {
            Text      = "🔄",
            Location  = new Point(475, 64),
            Size      = new Size(40, 34),
            BackColor = AppTheme.Surface,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.DarkText
        };
        _btnRefresh.FlatAppearance.BorderColor = AppTheme.Border;
        _btnRefresh.Click += (_, _) => LoadCounts();

        _grid = new DataGridView
        {
            Location               = new Point(20, 115),
            Size                   = new Size(860, 440),
            BackgroundColor        = AppTheme.Surface,
            GridColor              = AppTheme.Border,
            BorderStyle            = BorderStyle.None,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Primary, ForeColor = Color.White, Font = AppTheme.SectionFont, Padding = new Padding(6) },
            DefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, Padding = new Padding(4), SelectionBackColor = AppTheme.Secondary, SelectionForeColor = AppTheme.DarkText },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.Background },
            RowHeadersVisible      = false,
            AllowUserToAddRows     = false,
            AllowUserToDeleteRows  = false,
            ReadOnly               = true,
            SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate            = { Height = 32 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight    = 38
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Num",    HeaderText = "رقم الجرد",   Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WH",     HeaderText = "المستودع",    Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",   HeaderText = "التاريخ",     Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة",      Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "By",     HeaderText = "أنشأه",       Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Approved", HeaderText = "اعتمده",    Width = 140 });
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenCount(false); };

        _btnOpen = new Button
        {
            Text      = "📂 فتح / تعديل",
            Location  = new Point(20, 570),
            Size      = new Size(150, 36),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.Click += (_, _) => OpenCount(false);

        _btnReport = new Button
        {
            Text      = "📊 تقرير الفروق",
            Location  = new Point(185, 570),
            Size      = new Size(150, 36),
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.DarkText,
            FlatStyle = FlatStyle.Flat
        };
        _btnReport.FlatAppearance.BorderColor = AppTheme.Border;
        _btnReport.Click += OnReport;

        Controls.AddRange(new Control[]
        {
            _lblTitle, _lblWH, _cmbWH, _btnNew, _btnRefresh,
            _grid, _btnOpen, _btnReport
        });
    }

    private void LoadWarehouses()
    {
        try
        {
            _warehouses = _repo.GetAll();
            _cmbWH.Items.Clear();
            _cmbWH.Items.Add("كل المستودعات");
            foreach (var w in _warehouses) _cmbWH.Items.Add(w.Name);
            _cmbWH.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل المستودعات:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadCounts()
    {
        try
        {
            int? whId = null;
            if (_cmbWH.SelectedIndex > 0)
                whId = _warehouses[_cmbWH.SelectedIndex - 1].Id;

            _counts = _repo.GetInventoryCounts(whId);
            _grid.Rows.Clear();
            foreach (var c in _counts)
            {
                var row = _grid.Rows.Add(c.CountNumber, c.WarehouseName,
                    c.CountDate.ToString("yyyy-MM-dd"), c.StatusAr, c.CreatedBy, c.ApprovedBy);
                _grid.Rows[row].Tag = c;
                if (c.Status == "approved")
                    _grid.Rows[row].DefaultCellStyle.ForeColor = AppTheme.Success;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل الجرد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnNew(object? sender, EventArgs e)
    {
        if (_cmbWH.SelectedIndex <= 0)
        {
            MessageBox.Show("اختر مستودعاً أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var wh = _warehouses[_cmbWH.SelectedIndex - 1];
        try
        {
            var notes = Microsoft.VisualBasic.Interaction.InputBox("ملاحظات (اختياري):", "جرد جديد", "");
            var countId = _repo.CreateInventoryCount(wh.Id, notes, SessionContext.CurrentUser!.Id);
            var dlg = new InventoryCountDialog(countId, false);
            dlg.ShowDialog(this);
            LoadCounts();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في إنشاء الجرد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenCount(bool readOnly)
    {
        if (_grid.SelectedRows.Count == 0) return;
        var count = (InventoryCount)_grid.SelectedRows[0].Tag!;
        bool ro   = readOnly || count.Status == "approved";
        var dlg   = new InventoryCountDialog(count.Id, ro);
        dlg.ShowDialog(this);
        LoadCounts();
    }

    private void OnReport(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;
        var count = (InventoryCount)_grid.SelectedRows[0].Tag!;
        try
        {
            var lines = _repo.GetCountLines(count.Id);
            var diffs = lines.Where(l => l.Difference != 0).ToList();
            if (!diffs.Any())
            {
                MessageBox.Show("لا توجد فروق في هذه الجلسة.", "تقرير الفروق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"تقرير الفروق — {count.CountNumber}");
            sb.AppendLine($"المستودع: {count.WarehouseName}  |  التاريخ: {count.CountDate:yyyy-MM-dd}");
            sb.AppendLine(new string('─', 70));
            sb.AppendLine($"{"الصنف",-30}{"النظام",10}{"المحسوب",12}{"الفرق",10}");
            sb.AppendLine(new string('─', 70));
            foreach (var l in diffs)
                sb.AppendLine($"{l.ItemName,-30}{l.SystemQty,10:N2}{l.CountedQty,12:N2}{l.Difference,10:N2}");
            sb.AppendLine(new string('─', 70));
            sb.AppendLine($"إجمالي الأصناف بفروق: {diffs.Count}");

            MessageBox.Show(sb.ToString(), "تقرير الفروق", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في التقرير:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

