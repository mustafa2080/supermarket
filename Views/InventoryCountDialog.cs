using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>نموذج إنشاء جلسة جرد وإدخال الكميات المحسوبة — TASK-019</summary>
internal class InventoryCountDialog : Form
{
    // ── حقن ──────────────────────────────────────────────────
    private readonly WarehouseRepository _repo = new();
    private readonly int   _countId;
    private readonly bool  _readOnly;

    // ── عناصر ────────────────────────────────────────────────
    private Label   _lblTitle = null!;
    private Label   _lblSearch = null!;
    private TextBox _txtSearch = null!;
    private Label   _lblFilter = null!;
    private ComboBox _cmbFilter = null!;
    private DataGridView _grid = null!;
    private Label   _lblSummary = null!;
    private Button  _btnSave = null!;
    private Button  _btnApprove = null!;
    private Button  _btnClose = null!;

    private List<InventoryCountLine> _lines = new();

    // ── منشئ ─────────────────────────────────────────────────
    public InventoryCountDialog(int countId, bool readOnly = false)
    {
        _countId  = countId;
        _readOnly = readOnly;
        InitializeComponent();
        LoadData();
    }

    // ── واجهة ─────────────────────────────────────────────────
    private void InitializeComponent()
    {
        Text            = _readOnly ? "📋 عرض جلسة الجرد" : "📦 إدخال كميات الجرد";
        Size            = new Size(900, 660);
        StartPosition   = FormStartPosition.CenterParent;
        MinimizeBox     = false;
        MaximizeBox     = false;
        RightToLeft     = RightToLeft.Yes;
                BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;

        // عنوان
        _lblTitle = new Label
        {
            Text      = "جرد المخزون",
            Font      = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary,
            AutoSize  = true,
            Location  = new Point(20, 16)
        };

        // بحث
        _lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(20, 60), ForeColor = AppTheme.DarkText };
        _txtSearch = new TextBox { Location = new Point(70, 57), Width = 200, BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _txtSearch.TextChanged += (_, _) => FilterGrid();

        // فلتر الفروق
        _lblFilter = new Label { Text = "عرض:", AutoSize = true, Location = new Point(290, 60), ForeColor = AppTheme.DarkText };
        _cmbFilter = new ComboBox
        {
            Location      = new Point(330, 57),
            Width         = 140,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor     = AppTheme.Surface,
            ForeColor     = AppTheme.DarkText
        };
        _cmbFilter.Items.AddRange(new object[] { "الكل", "فروق فقط", "زيادة", "نقصان" });
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) => FilterGrid();

        // جدول
        _grid = new DataGridView
        {
            Location               = new Point(20, 95),
            Size                   = new Size(840, 450),
            BackgroundColor        = AppTheme.Surface,
            GridColor              = AppTheme.Border,
            BorderStyle            = BorderStyle.None,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Primary, ForeColor = Color.White, Font = AppTheme.SectionFont, Padding = new Padding(6) },
            DefaultCellStyle       = new DataGridViewCellStyle
                { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, Padding = new Padding(4), SelectionBackColor = AppTheme.Secondary, SelectionForeColor = AppTheme.DarkText },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.Background },
            RowHeadersVisible      = false,
            AutoSizeRowsMode       = DataGridViewAutoSizeRowsMode.None,
            RowTemplate            = { Height = 32 },
            AllowUserToAddRows     = false,
            AllowUserToDeleteRows  = false,
            SelectionMode          = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly               = _readOnly,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight    = 38
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemCode", HeaderText = "كود", Width = 90, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemName", HeaderText = "الصنف", Width = 230, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",     HeaderText = "الوحدة", Width = 80, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SysQty",   HeaderText = "كمية النظام", Width = 110, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CountedQty", HeaderText = "الكمية المحسوبة", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diff",     HeaderText = "الفرق", Width = 90, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiffIcon", HeaderText = "", Width = 50, ReadOnly = true });

        if (!_readOnly)
        {
            _grid.CellEndEdit    += OnCellEndEdit;
            _grid.EditingControlShowing += OnEditingControlShowing;
        }

        // ملخص
        _lblSummary = new Label
        {
            Text      = "",
            AutoSize  = true,
            Location  = new Point(20, 555),
            ForeColor = AppTheme.MutedText,
            Font      = AppTheme.SectionFont
        };

        // أزرار
        _btnSave = new Button
        {
            Text      = "💾 حفظ الكميات",
            Location  = new Point(20, 590),
            Size      = new Size(150, 36),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Visible   = !_readOnly
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += OnSave;

        _btnApprove = new Button
        {
            Text      = "✅ اعتماد الجرد",
            Location  = new Point(185, 590),
            Size      = new Size(150, 36),
            BackColor = AppTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Visible   = !_readOnly && SessionContext.CurrentUser?.RoleName == "admin"
        };
        _btnApprove.FlatAppearance.BorderSize = 0;
        _btnApprove.Click += OnApprove;

        _btnClose = new Button
        {
            Text      = "إغلاق",
            Location  = new Point(730, 590),
            Size      = new Size(130, 36),
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.DarkText,
            FlatStyle = FlatStyle.Flat
        };
        _btnClose.FlatAppearance.BorderColor = AppTheme.Border;
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            _lblTitle, _lblSearch, _txtSearch, _lblFilter, _cmbFilter,
            _grid, _lblSummary, _btnSave, _btnApprove, _btnClose
        });
    }

    // ── تحميل البيانات ────────────────────────────────────────
    private void LoadData()
    {
        try
        {
            _lines = _repo.GetCountLines(_countId);
            BindGrid(_lines);
            UpdateSummary();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل سطور الجرد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid(List<InventoryCountLine> lines)
    {
        _grid.Rows.Clear();
        foreach (var l in lines)
        {
            var diff = l.Difference;
            var icon = diff > 0 ? "🔼" : diff < 0 ? "🔽" : "—";
            var row  = _grid.Rows.Add(l.ItemCode, l.ItemName, l.UnitName,
                                       l.SystemQty.ToString("N2"),
                                       l.CountedQty.ToString("N2"),
                                       diff.ToString("N2"), icon);
            _grid.Rows[row].Tag = l;
            // تلوين
            if (diff < 0)      _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
            else if (diff > 0) _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
        }
    }

    private void FilterGrid()
    {
        var q      = _txtSearch.Text.Trim().ToLower();
        var filter = _cmbFilter.SelectedItem?.ToString() ?? "الكل";

        var filtered = _lines.Where(l =>
        {
            if (!string.IsNullOrEmpty(q) && !l.ItemName.ToLower().Contains(q) && !l.ItemCode.ToLower().Contains(q))
                return false;
            return filter switch
            {
                "فروق فقط" => l.Difference != 0,
                "زيادة"    => l.Difference > 0,
                "نقصان"    => l.Difference < 0,
                _          => true
            };
        }).ToList();

        BindGrid(filtered);
    }

    private void UpdateSummary()
    {
        int total    = _lines.Count;
        int withDiff = _lines.Count(l => l.Difference != 0);
        int surplus  = _lines.Count(l => l.Difference > 0);
        int deficit  = _lines.Count(l => l.Difference < 0);
        _lblSummary.Text = $"إجمالي الأصناف: {total}  |  لها فروق: {withDiff}  |  زيادة: {surplus}  |  نقصان: {deficit}";
    }

    // ── تعديل خلية ───────────────────────────────────────────
    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != _grid.Columns["CountedQty"]!.Index) return;
        var row = _grid.Rows[e.RowIndex];
        var line = (InventoryCountLine)row.Tag!;

        if (decimal.TryParse(row.Cells["CountedQty"].Value?.ToString(), out var qty))
        {
            line.CountedQty = qty;
            var diff = line.Difference;
            row.Cells["Diff"].Value    = diff.ToString("N2");
            row.Cells["DiffIcon"].Value = diff > 0 ? "🔼" : diff < 0 ? "🔽" : "—";
            row.DefaultCellStyle.BackColor =
                diff < 0 ? Color.FromArgb(255, 235, 238) :
                diff > 0 ? Color.FromArgb(232, 245, 233) : AppTheme.Surface;
        }
        UpdateSummary();
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_grid.CurrentCell?.OwningColumn.Name == "CountedQty" && e.Control is TextBox tb)
        {
            tb.KeyPress -= NumericOnly;
            tb.KeyPress += NumericOnly;
        }
    }

    private static void NumericOnly(object? s, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            e.Handled = true;
    }

    // ── حفظ ──────────────────────────────────────────────────
    private void OnSave(object? sender, EventArgs e)
    {
        try
        {
            var updates = _lines.Select(l => (l.ItemId, l.CountedQty)).ToList();
            _repo.SaveCountLines(_countId, updates);
            MessageBox.Show("✅ تم حفظ الكميات بنجاح.", "حفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── اعتماد ───────────────────────────────────────────────
    private void OnApprove(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
            "⚠️ بعد الاعتماد لن يمكن تعديل الجرد.\nهل تريد اعتماد الجرد وتحديث الأرصدة الفعلية؟",
            "تأكيد الاعتماد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            // حفظ أولاً ثم اعتماد
            var updates = _lines.Select(l => (l.ItemId, l.CountedQty)).ToList();
            _repo.SaveCountLines(_countId, updates);
            _repo.ApproveInventoryCount(_countId, SessionContext.CurrentUser!.Id);

            MessageBox.Show("✅ تم اعتماد الجرد وتحديث أرصدة المخزون.", "اعتماد", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الاعتماد:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

