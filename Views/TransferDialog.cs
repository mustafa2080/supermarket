using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>نموذج إنشاء/تعديل تحويل مستودع — TASK-020</summary>
internal class TransferDialog : Form
{
    private readonly WarehouseRepository _repo = new();
    private readonly int  _transferId;
    private readonly bool _readOnly;

    // عناصر الرأس
    private Label    _lblFrom   = null!;
    private Label    _lblFromVal= null!;
    private Label    _lblTo     = null!;
    private Label    _lblToVal  = null!;
    private Label    _lblDate   = null!;
    private Label    _lblDateVal= null!;

    // جدول الأصناف
    private TextBox  _txtSearch = null!;
    private DataGridView _grid  = null!;
    private Label    _lblTotal  = null!;

    // أزرار
    private Button _btnSave    = null!;
    private Button _btnApprove = null!;
    private Button _btnClose   = null!;

    private List<StockLevel>    _stock = new();   // رصيد المستودع المصدر
    private List<TransferLine>  _saved = new();   // سطور محفوظة (إن وجدت)
    private int _fromWhId;

    public TransferDialog(int transferId, bool readOnly = false)
    {
        _transferId = transferId;
        _readOnly   = readOnly;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text              = _readOnly ? "📋 عرض التحويل" : "🔄 تحويل بين المستودعات";
        Size              = new Size(920, 680);
        StartPosition     = FormStartPosition.CenterParent;
        MinimizeBox       = false;
        MaximizeBox       = false;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;

        // ── رأس التحويل ─────────────────────────────────────
        var pnlHeader = new Panel
        {
            Location  = new Point(20, 16),
            Size      = new Size(860, 50),
            BackColor = AppTheme.Surface
        };
        void AddHdr(string caption, ref Label lbl, ref Label val, int x)
        {
            var lc = new Label { Text = caption, AutoSize = true, Location = new Point(x, 8),
                                 ForeColor = AppTheme.MutedText, Font = AppTheme.BodyFont };
            var lv = new Label { Text = "—", AutoSize = true, Location = new Point(x, 26),
                                 ForeColor = AppTheme.DarkText, Font = AppTheme.SectionFont };
            pnlHeader.Controls.Add(lc);
            pnlHeader.Controls.Add(lv);
            lbl = lc; val = lv;
        }
        AddHdr("من مستودع:", ref _lblFrom,  ref _lblFromVal, 10);
        AddHdr("إلى مستودع:", ref _lblTo,   ref _lblToVal,   300);
        AddHdr("التاريخ:",    ref _lblDate,  ref _lblDateVal, 590);

        // بحث
        var lblSearch = new Label { Text = "بحث:", AutoSize = true,
            Location = new Point(20, 80), ForeColor = AppTheme.DarkText };
        _txtSearch = new TextBox { Location = new Point(75, 77), Width = 220,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _txtSearch.TextChanged += (_, _) => FilterGrid();

        // جدول
        _grid = new DataGridView
        {
            Location              = new Point(20, 112),
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
                { BackColor = AppTheme.Primary, ForeColor = Color.White,
                  Font = AppTheme.SectionFont, Padding = new Padding(6) },
            DefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
                  Padding = new Padding(4),
                  SelectionBackColor = AppTheme.Secondary,
                  SelectionForeColor = Color.White },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Background }
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code",     HeaderText = "كود",        Width = 90,  ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",     HeaderText = "الصنف",      Width = 240, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit",     HeaderText = "الوحدة",     Width = 80,  ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Available",HeaderText = "المتاح",     Width = 100, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Transfer", HeaderText = "كمية التحويل", Width = 120 });

        if (!_readOnly)
        {
            _grid.CellEndEdit += OnCellEndEdit;
            _grid.EditingControlShowing += OnEditingControlShowing;
        }

        // إجمالي
        _lblTotal = new Label
        {
            Text      = "إجمالي أصناف التحويل: 0",
            AutoSize  = true,
            Location  = new Point(20, 572),
            ForeColor = AppTheme.MutedText,
            Font      = AppTheme.SectionFont
        };

        // أزرار
        _btnSave = new Button
        {
            Text = "💾 حفظ", Location = new Point(20, 606), Size = new Size(120, 36),
            BackColor = AppTheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Visible = !_readOnly
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += OnSave;

        _btnApprove = new Button
        {
            Text = "✅ اعتماد", Location = new Point(155, 606), Size = new Size(130, 36),
            BackColor = AppTheme.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Visible = !_readOnly && SessionContext.IsAdmin
        };
        _btnApprove.FlatAppearance.BorderSize = 0;
        _btnApprove.Click += OnApprove;

        _btnClose = new Button
        {
            Text = "إغلاق", Location = new Point(760, 606), Size = new Size(120, 36),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat
        };
        _btnClose.FlatAppearance.BorderColor = AppTheme.Border;
        _btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            pnlHeader, lblSearch, _txtSearch,
            _grid, _lblTotal, _btnSave, _btnApprove, _btnClose
        });
    }

    private void LoadData()
    {
        try
        {
            // جلب بيانات التحويل من القائمة
            var transfers = _repo.GetTransfers();
            var t = transfers.FirstOrDefault(x => x.Id == _transferId);
            if (t == null) return;

            _fromWhId        = t.FromWarehouseId;
            _lblFromVal.Text = t.FromWarehouse;
            _lblToVal.Text   = t.ToWarehouse;
            _lblDateVal.Text = t.TransferDate.ToString("yyyy-MM-dd");

            // رصيد المستودع المصدر
            _stock = _repo.GetAvailableStock(_fromWhId);

            // سطور محفوظة إن وجدت
            _saved = _repo.GetTransferLines(_transferId);

            BindGrid(_stock);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل بيانات التحويل:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid(List<StockLevel> items)
    {
        _grid.Rows.Clear();
        foreach (var s in items)
        {
            // كمية التحويل المحفوظة إن وجدت
            var saved = _saved.FirstOrDefault(l => l.ItemId == s.ItemId);
            var tQty  = saved?.Quantity ?? 0m;

            var row = _grid.Rows.Add(s.ItemCode, s.ItemName, s.UnitName,
                                      s.Quantity.ToString("N2"),
                                      tQty > 0 ? tQty.ToString("N2") : "");
            _grid.Rows[row].Tag = s;
            if (tQty > 0)
                _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
        }
        UpdateTotal();
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
        if (e.ColumnIndex != _grid.Columns["Transfer"]!.Index) return;
        var row  = _grid.Rows[e.RowIndex];
        var stock = (StockLevel)row.Tag!;

        var raw = row.Cells["Transfer"].Value?.ToString() ?? "";
        if (!decimal.TryParse(raw, out var qty) || qty < 0) qty = 0;

        if (qty > stock.Quantity)
        {
            MessageBox.Show($"الكمية ({qty}) تتجاوز المتاح ({stock.Quantity:N2}).",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            qty = 0;
        }

        row.Cells["Transfer"].Value = qty > 0 ? qty.ToString("N2") : "";
        row.DefaultCellStyle.BackColor = qty > 0
            ? Color.FromArgb(232, 245, 233) : AppTheme.Surface;

        // تحديث الـ _saved في الذاكرة
        var existing = _saved.FirstOrDefault(l => l.ItemId == stock.ItemId);
        if (existing != null) existing.Quantity = qty;
        else if (qty > 0) _saved.Add(new TransferLine { ItemId = stock.ItemId, Quantity = qty });

        UpdateTotal();
    }

    private void OnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_grid.CurrentCell?.OwningColumn.Name == "Transfer" && e.Control is TextBox tb)
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

    private void UpdateTotal()
    {
        int cnt = _grid.Rows.Cast<DataGridViewRow>()
            .Count(r => !string.IsNullOrEmpty(r.Cells["Transfer"].Value?.ToString()));
        _lblTotal.Text = $"إجمالي أصناف التحويل: {cnt}";
    }

    private List<(int itemId, decimal qty)> CollectLines()
    {
        var list = new List<(int, decimal)>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var stock = (StockLevel)row.Tag!;
            var raw   = row.Cells["Transfer"].Value?.ToString() ?? "";
            if (decimal.TryParse(raw, out var qty) && qty > 0)
                list.Add((stock.ItemId, qty));
        }
        return list;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        var lines = CollectLines();
        if (!lines.Any())
        {
            MessageBox.Show("أدخل كمية لصنف واحد على الأقل.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _repo.SaveTransferLines(_transferId, lines);
            MessageBox.Show("✅ تم حفظ التحويل بنجاح.", "حفظ",
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
            "بعد الاعتماد ستُحدَّث أرصدة المستودعين فوراً ولا يمكن التراجع.\nتأكيد الاعتماد؟",
            "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            var lines = CollectLines();
            _repo.SaveTransferLines(_transferId, lines);
            _repo.ApproveTransfer(_transferId, SessionContext.CurrentUser!.Id);
            MessageBox.Show("✅ تم اعتماد التحويل وتحديث الأرصدة.", "اعتماد",
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
