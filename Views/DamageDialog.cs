using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>نموذج إنشاء/تعديل سجل تالف — TASK-021</summary>
internal class DamageDialog : Form
{
    private readonly WarehouseRepository _repo = new();
    private readonly int  _recordId;
    private readonly bool _readOnly;

    // رأس السجل
    private Label    _lblWhVal     = null!;
    private Label    _lblReasonVal = null!;
    private Label    _lblDateVal   = null!;

    // جدول
    private TextBox      _txtSearch = null!;
    private DataGridView _grid      = null!;
    private Label        _lblTotal  = null!;

    // أزرار
    private Button _btnSave    = null!;
    private Button _btnApprove = null!;
    private Button _btnClose   = null!;

    private List<StockLevel>  _stock = new();
    private List<DamageLine>  _saved = new();
    private int _whId;

    public DamageDialog(int recordId, bool readOnly = false)
    {
        _recordId = recordId;
        _readOnly = readOnly;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text              = _readOnly ? "📋 عرض سجل التالف" : "🗑️ تسجيل التالف والمُهلَك";
        Size              = new Size(920, 680);
        StartPosition     = FormStartPosition.CenterParent;
        MinimizeBox       = false; MaximizeBox = false;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;

        // ── رأس السجل ───────────────────────────────────────
        var pnlHdr = new Panel { Location = new Point(20, 16), Size = new Size(860, 52), BackColor = AppTheme.Surface };

        void Hdr(string caption, ref Label val, int x)
        {
            pnlHdr.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(x, 6),
                ForeColor = AppTheme.MutedText });
            val = new Label { Text = "—", AutoSize = true, Location = new Point(x, 26),
                ForeColor = AppTheme.DarkText, Font = AppTheme.SectionFont };
            pnlHdr.Controls.Add(val);
        }
        Hdr("المستودع:", ref _lblWhVal, 10);
        Hdr("سبب التلف:", ref _lblReasonVal, 280);
        Hdr("التاريخ:", ref _lblDateVal, 560);

        // بحث
        var lblSrch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(20, 82),
            ForeColor = AppTheme.DarkText };
        _txtSearch = new TextBox { Location = new Point(75, 79), Width = 230,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _txtSearch.TextChanged += (_, _) => FilterGrid();

        // جدول
        _grid = new DataGridView
        {
            Location              = new Point(20, 114),
            Size                  = new Size(860, 450),
            BackgroundColor       = AppTheme.Surface,
            GridColor             = AppTheme.Border,
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly              = _readOnly,
            RowTemplate           = { Height = 32 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 38,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Danger, ForeColor = Color.White,
                  Font = AppTheme.SectionFont, Padding = new Padding(6) },
            DefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
                  Padding = new Padding(4),
                  SelectionBackColor = Color.FromArgb(255, 205, 210),
                  SelectionForeColor = AppTheme.DarkText },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AppTheme.Background }
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code",  HeaderText = "كود",        Width = 90,  ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",  HeaderText = "الصنف",      Width = 240, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",  HeaderText = "الوحدة",     Width = 80,  ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "الرصيد",     Width = 90,  ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty",   HeaderText = "كمية التالف", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost",  HeaderText = "سعر الشراء", Width = 110, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "القيمة",     Width = 110, ReadOnly = true });

        if (!_readOnly)
        {
            _grid.CellEndEdit += OnCellEndEdit;
            _grid.EditingControlShowing += OnEditingControlShowing;
        }

        // إجمالي
        _lblTotal = new Label
        {
            Text      = "إجمالي قيمة التلف: 0.00 ج.م",
            AutoSize  = true,
            Location  = new Point(20, 574),
            ForeColor = AppTheme.Danger,
            Font      = AppTheme.SectionFont
        };

        // أزرار
        _btnSave = new Button
        {
            Text = "💾 حفظ", Location = new Point(20, 608), Size = new Size(120, 36),
            BackColor = AppTheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Visible = !_readOnly
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += OnSave;

        _btnApprove = new Button
        {
            Text = "✅ اعتماد", Location = new Point(155, 608), Size = new Size(130, 36),
            BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Visible = !_readOnly && SessionContext.IsAdmin
        };
        _btnApprove.FlatAppearance.BorderSize = 0;
        _btnApprove.Click += OnApprove;

        _btnClose = new Button
        {
            Text = "إغلاق", Location = new Point(760, 608), Size = new Size(120, 36),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat
        };
        _btnClose.FlatAppearance.BorderColor = AppTheme.Border;
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            pnlHdr, lblSrch, _txtSearch,
            _grid, _lblTotal, _btnSave, _btnApprove, _btnClose
        });
    }

    private void LoadData()
    {
        try
        {
            var records = _repo.GetDamageRecords();
            var rec = records.FirstOrDefault(x => x.Id == _recordId);
            if (rec == null) return;

            _whId              = rec.WarehouseId;
            _lblWhVal.Text     = rec.WarehouseName;
            _lblReasonVal.Text = rec.ReasonAr;
            _lblDateVal.Text   = rec.RecordDate.ToString("yyyy-MM-dd");

            _stock = _repo.GetAvailableStock(_whId);
            _saved = _repo.GetDamageLines(_recordId);

            BindGrid(_stock);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل البيانات:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid(List<StockLevel> items)
    {
        _grid.Rows.Clear();
        decimal grandTotal = 0;
        foreach (var s in items)
        {
            var saved   = _saved.FirstOrDefault(l => l.ItemId == s.ItemId);
            var qty     = saved?.Quantity ?? 0m;
            var cost    = s.PurchasePrice;
            var lineVal = qty * cost;
            grandTotal += lineVal;

            var row = _grid.Rows.Add(
                s.ItemCode, s.ItemName, s.UnitName,
                s.Quantity.ToString("N2"),
                qty > 0 ? qty.ToString("N2") : "",
                cost.ToString("N2"),
                qty > 0 ? lineVal.ToString("N2") : "");
            _grid.Rows[row].Tag = s;
            if (qty > 0)
                _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
        }
        _lblTotal.Text = $"إجمالي قيمة التلف: {grandTotal:N2} ج.م";
    }

    private void FilterGrid()
    {
        var q = _txtSearch.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(q)) { BindGrid(_stock); return; }
        BindGrid(_stock.Where(s =>
            s.ItemName.ToLower().Contains(q) || s.ItemCode.ToLower().Contains(q)).ToList());
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != _grid.Columns["Qty"]!.Index) return;
        var row   = _grid.Rows[e.RowIndex];
        var stock = (StockLevel)row.Tag!;

        var raw = row.Cells["Qty"].Value?.ToString() ?? "";
        if (!decimal.TryParse(raw, out var qty) || qty < 0) qty = 0;

        if (qty > stock.Quantity)
        {
            MessageBox.Show($"الكمية ({qty}) تتجاوز الرصيد المتاح ({stock.Quantity:N2}).",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            qty = 0;
        }

        var lineVal = qty * stock.PurchasePrice;
        row.Cells["Qty"].Value   = qty > 0 ? qty.ToString("N2") : "";
        row.Cells["Total"].Value = qty > 0 ? lineVal.ToString("N2") : "";
        row.DefaultCellStyle.BackColor = qty > 0
            ? Color.FromArgb(255, 235, 238) : AppTheme.Surface;

        // تحديث الذاكرة
        var existing = _saved.FirstOrDefault(l => l.ItemId == stock.ItemId);
        if (existing != null) existing.Quantity = qty;
        else if (qty > 0) _saved.Add(new DamageLine { ItemId = stock.ItemId, Quantity = qty, UnitCost = stock.PurchasePrice });

        // إعادة حساب الإجمالي
        decimal total = _saved.Sum(l => l.TotalValue);
        _lblTotal.Text = $"إجمالي قيمة التلف: {total:N2} ج.م";
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_grid.CurrentCell?.OwningColumn.Name == "Qty" && e.Control is TextBox tb)
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

    private List<(int, decimal, decimal)> CollectLines()
    {
        var list = new List<(int, decimal, decimal)>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var s   = (StockLevel)row.Tag!;
            var raw = row.Cells["Qty"].Value?.ToString() ?? "";
            if (decimal.TryParse(raw, out var qty) && qty > 0)
                list.Add((s.ItemId, qty, s.PurchasePrice));
        }
        return list;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        var lines = CollectLines();
        if (!lines.Any())
        {
            MessageBox.Show("أدخل كمية لصنف تالف واحد على الأقل.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _repo.SaveDamageLines(_recordId, lines);
            MessageBox.Show("✅ تم حفظ سجل التالف.", "حفظ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الحفظ:\n" + ex.Message, "خطأ",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnApprove(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
            "⚠️ بعد الاعتماد ستُخصم الكميات من المخزون ولا يمكن التراجع.\nتأكيد الاعتماد؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            var lines = CollectLines();
            _repo.SaveDamageLines(_recordId, lines);
            _repo.ApproveDamageRecord(_recordId, SessionContext.CurrentUser!.Id);
            MessageBox.Show("✅ تم اعتماد سجل التالف وخصم المخزون.", "اعتماد",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الاعتماد:\n" + ex.Message, "خطأ",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
