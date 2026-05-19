using System.Drawing;
using Npgsql;
using supermarket.Data;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-013 — نموذج مرتجع المشتريات
/// يدعم: مرتجع كامل من فاتورة معتمدة، مرتجع جزئي، مرتجع مستقل
/// </summary>
internal sealed class PurchaseReturnDialog : Form
{
    // ── رأس المرتجع ──────────────────────────────────────────
    private readonly TextBox        _numBox;
    private readonly DateTimePicker _datePicker;
    private readonly ComboBox       _supplierCombo;
    private readonly ComboBox       _warehouseCombo;
    private readonly TextBox        _notesBox;

    // ── اختيار الفاتورة الأصلية ──────────────────────────────
    private readonly TextBox        _invoiceSearchBox;
    private readonly Button         _invoiceLoadBtn;
    private readonly Label          _invoiceInfoLbl;

    // ── شبكة السطور ──────────────────────────────────────────
    private readonly DataGridView   _linesGrid;

    // ── الإجمالي وأزرار الحفظ ────────────────────────────────
    private readonly Label          _totalLbl;
    private readonly Button         _saveBtn;
    private readonly Button         _approveBtn;
    private readonly Label          _feedbackLbl;

    // ── البيانات ─────────────────────────────────────────────
    private readonly PurchaseReturn        _return;
    private readonly List<Supplier>        _suppliers;
    private readonly List<Warehouse>       _warehouses;
    private PurchaseInvoice?               _sourceInvoice;

    public PurchaseReturnDialog(PurchaseReturn? existing = null, PurchaseInvoice? sourceInvoice = null)
    {
        _numBox           = AppTheme.CreateTextBox();
        _datePicker       = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _supplierCombo    = AppTheme.CreateComboBox();
        _warehouseCombo   = AppTheme.CreateComboBox();
        _notesBox         = AppTheme.CreateTextBox("ملاحظات...");
        _invoiceSearchBox = AppTheme.CreateTextBox("رقم الفاتورة الأصلية (PUR-XXXXX)...");
        _invoiceLoadBtn   = new Button { Text = "📂 تحميل" };
        _invoiceInfoLbl   = new Label();
        _linesGrid        = new DataGridView();
        _totalLbl         = new Label();
        _saveBtn          = new Button { Text = "💾 حفظ مسودة" };
        _approveBtn       = new Button { Text = "✅ اعتماد المرتجع" };
        _feedbackLbl      = new Label();

        _return     = existing ?? new PurchaseReturn();
        _suppliers  = new SupplierRepository().GetAll(activeOnly: true);
        _warehouses = LoadWarehouses();
        _sourceInvoice = sourceInvoice;

        Text              = existing is null ? "مرتجع مشتريات جديد" : $"مرتجع: {existing.ReturnNumber}";
        Size              = new Size(1100, 720);
        MinimumSize       = new Size(900, 600);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;

        BuildUI();
        WireEvents();
        LoadInitialData();
    }

    // ══════════════════════════════════════════════════════════
    // بناء الواجهة
    // ══════════════════════════════════════════════════════════
    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(10), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // رأس
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));  // شريط الفاتورة
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // الشبكة
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // الإجمالي
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));  // الأزرار

        root.Controls.Add(BuildHeaderPanel(),   0, 0);
        root.Controls.Add(BuildInvoiceBar(),    0, 1);
        root.Controls.Add(BuildLinesGrid(),     0, 2);
        root.Controls.Add(BuildTotalBar(),      0, 3);
        root.Controls.Add(BuildButtonBar(),     0, 4);
        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2
        };
        for (int i = 0; i < 5; i++)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        tbl.Controls.Add(AppTheme.CreateFieldLabel("رقم المرتجع:"), 0, 0);
        _numBox.Dock = DockStyle.Fill; _numBox.ReadOnly = true;
        tbl.Controls.Add(_numBox, 0, 1);

        tbl.Controls.Add(AppTheme.CreateFieldLabel("التاريخ:"), 1, 0);
        _datePicker.Dock = DockStyle.Fill;
        tbl.Controls.Add(_datePicker, 1, 1);

        tbl.Controls.Add(AppTheme.CreateFieldLabel("المورد: *"), 2, 0);
        _supplierCombo.Dock = DockStyle.Fill;
        tbl.Controls.Add(_supplierCombo, 2, 1);

        tbl.Controls.Add(AppTheme.CreateFieldLabel("المستودع: *"), 3, 0);
        _warehouseCombo.Dock = DockStyle.Fill;
        tbl.Controls.Add(_warehouseCombo, 3, 1);

        tbl.Controls.Add(AppTheme.CreateFieldLabel("ملاحظات:"), 4, 0);
        _notesBox.Dock = DockStyle.Fill;
        tbl.Controls.Add(_notesBox, 4, 1);

        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildInvoiceBar()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4, 2, 4, 2);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var lbl = AppTheme.CreateFieldLabel("الفاتورة الأصلية:");
        lbl.Margin = new Padding(0, 8, 8, 0);

        _invoiceSearchBox.Width = 220; _invoiceSearchBox.Margin = new Padding(4, 6, 4, 0);
        AppTheme.StyleSecondaryButton(_invoiceLoadBtn);
        _invoiceLoadBtn.Width  = 110; _invoiceLoadBtn.Margin   = new Padding(4, 4, 4, 0);

        _invoiceInfoLbl.AutoSize  = true;
        _invoiceInfoLbl.Margin    = new Padding(12, 9, 4, 0);
        _invoiceInfoLbl.Font      = AppTheme.SmallFont;
        _invoiceInfoLbl.ForeColor = AppTheme.MutedText;
        _invoiceInfoLbl.Text      = "اختياري — اتركه فارغاً للمرتجع المستقل";

        flow.Controls.Add(lbl);
        flow.Controls.Add(_invoiceSearchBox);
        flow.Controls.Add(_invoiceLoadBtn);
        flow.Controls.Add(_invoiceInfoLbl);

        card.Controls.Add(flow);
        return card;
    }

    private Control BuildLinesGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _linesGrid.Dock                  = DockStyle.Fill;
        _linesGrid.AllowUserToAddRows    = false;
        _linesGrid.AllowUserToDeleteRows = false;
        _linesGrid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _linesGrid.MultiSelect           = false;
        _linesGrid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _linesGrid.RowHeadersVisible     = false;
        _linesGrid.BorderStyle           = BorderStyle.None;
        _linesGrid.Font                  = AppTheme.BodyFont;
        _linesGrid.BackgroundColor       = AppTheme.Surface;
        _linesGrid.GridColor             = AppTheme.Border;
        _linesGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Danger;
        _linesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _linesGrid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _linesGrid.EnableHeadersVisualStyles = false;

        // الأعمدة: الكمية قابلة للتعديل، باقي read-only
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIdx",   HeaderText = "#",           Width = 40,  FillWeight = 3,  ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",  HeaderText = "الكود",       FillWeight = 9,  ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",  HeaderText = "الصنف",       FillWeight = 30, ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",  HeaderText = "الوحدة",      FillWeight = 9,  ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaxQty",HeaderText = "الكمية المتاحة",FillWeight = 12,ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",   HeaderText = "كمية الإرجاع", FillWeight = 13, ReadOnly = false }); // قابل للتعديل
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "السعر",       FillWeight = 10, ReadOnly = true });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي",    FillWeight = 12, ReadOnly = true });

        // زر حذف السطر
        _linesGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "colDel", HeaderText = "", Text = "🗑", UseColumnTextForButtonValue = true,
            Width = 40, FillWeight = 4
        });

        // تمييز عمود الكمية
        _linesGrid.Columns["colQty"]!.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FFF9C4");
        _linesGrid.Columns["colQty"]!.DefaultCellStyle.Font      = new Font("Tahoma", 10F, FontStyle.Bold);

        card.Controls.Add(_linesGrid);
        return card;
    }

    private Control BuildTotalBar()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4, 2, 4, 2);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _feedbackLbl.AutoSize  = true;
        _feedbackLbl.Margin    = new Padding(12, 10, 4, 0);
        _feedbackLbl.Font      = AppTheme.BodyFont;
        _feedbackLbl.ForeColor = AppTheme.MutedText;
        _feedbackLbl.Text      = "عدّل الكميات في العمود الأصفر ثم اضغط حفظ أو اعتماد.";

        var totalTitleLbl = AppTheme.CreateFieldLabel("💰 إجمالي المرتجع:");
        totalTitleLbl.Margin = new Padding(0, 10, 4, 0);
        totalTitleLbl.Font   = new Font("Tahoma", 11F, FontStyle.Bold);

        _totalLbl.AutoSize  = true;
        _totalLbl.Margin    = new Padding(4, 8, 4, 0);
        _totalLbl.Font      = new Font("Tahoma", 13F, FontStyle.Bold);
        _totalLbl.ForeColor = AppTheme.Danger;
        _totalLbl.Text      = "0.00";

        flow.Controls.Add(totalTitleLbl);
        flow.Controls.Add(_totalLbl);
        flow.Controls.Add(_feedbackLbl);

        card.Controls.Add(flow);
        return card;
    }

    private Control BuildButtonBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 4, 0, 0)
        };

        AppTheme.StyleSecondaryButton(_saveBtn);
        _saveBtn.Width = 160; _saveBtn.Margin = new Padding(6, 0, 0, 0);

        AppTheme.StylePrimaryButton(_approveBtn);
        _approveBtn.BackColor = AppTheme.Danger;
        _approveBtn.Width     = 180; _approveBtn.Margin = new Padding(6, 0, 0, 0);

        bar.Controls.Add(_approveBtn);
        bar.Controls.Add(_saveBtn);
        return bar;
    }

    // ══════════════════════════════════════════════════════════
    // تحميل البيانات الأولية
    // ══════════════════════════════════════════════════════════
    private void LoadInitialData()
    {
        // الموردين
        _supplierCombo.Items.Clear();
        _supplierCombo.Items.Add(new ComboItem(0, "-- اختر مورداً --"));
        foreach (var s in _suppliers)
            _supplierCombo.Items.Add(new ComboItem(s.Id, s.Name));
        _supplierCombo.SelectedIndex = 0;
        _supplierCombo.DisplayMember = "Name";
        _supplierCombo.ValueMember   = "Id";

        // المستودعات
        _warehouseCombo.Items.Clear();
        foreach (var w in _warehouses)
            _warehouseCombo.Items.Add(new ComboItem(w.Id, w.Name));
        _warehouseCombo.SelectedIndex = _warehouses.Count > 0 ? 0 : -1;
        _warehouseCombo.DisplayMember = "Name";
        _warehouseCombo.ValueMember   = "Id";

        if (_return.Id == 0)
        {
            _numBox.Text = new PurchaseReturnRepository().NextReturnNumber();
            _return.ReturnNumber = _numBox.Text;

            // لو جاء من شاشة الفاتورة مباشرة
            if (_sourceInvoice is not null)
                LoadInvoiceLines(_sourceInvoice);
        }
        else
        {
            // تعديل مرتجع موجود
            _numBox.Text      = _return.ReturnNumber;
            _datePicker.Value = _return.ReturnDate;
            SetComboById(_supplierCombo,  _return.SupplierId);
            SetComboById(_warehouseCombo, _return.WarehouseId);
            _notesBox.Text = _return.Notes;
            if (_return.OriginalInvoiceId.HasValue)
                _invoiceSearchBox.Text = _return.OriginalInvoiceNum;
            foreach (var l in _return.Lines)
                AddLineToGrid(l);
            RecalcTotal();

            bool editable = _return.Status == "draft";
            SetFormEditable(editable);
        }
    }

    private void LoadInvoiceLines(PurchaseInvoice invoice)
    {
        _sourceInvoice = invoice;
        _invoiceSearchBox.Text = invoice.InvoiceNumber;
        SetComboById(_supplierCombo,  invoice.SupplierId);
        SetComboById(_warehouseCombo, invoice.WarehouseId);
        _supplierCombo.Enabled  = false;
        _warehouseCombo.Enabled = false;

        _invoiceInfoLbl.Text      = $"✔ {invoice.SupplierName}  |  {invoice.InvoiceDate:dd/MM/yyyy}  |  {invoice.NetTotal:N2}";
        _invoiceInfoLbl.ForeColor = AppTheme.Success;

        // جلب السطور القابلة للإرجاع
        try
        {
            var repo  = new PurchaseReturnRepository();
            var lines = repo.GetReturnableLines(invoice.Id);

            if (lines.Count == 0)
            {
                ShowError("جميع أصناف هذه الفاتورة تم إرجاعها بالكامل.");
                return;
            }

            _linesGrid.Rows.Clear();
            _return.Lines.Clear();
            foreach (var l in lines)
            {
                _return.Lines.Add(l);
                AddLineToGrid(l);
            }
            RecalcTotal();
            ShowInfo($"تم تحميل {lines.Count} صنف — عدّل كميات الإرجاع في العمود الأصفر.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void SetFormEditable(bool editable)
    {
        _datePicker.Enabled       = editable;
        _notesBox.ReadOnly        = !editable;
        _invoiceSearchBox.Enabled = editable;
        _invoiceLoadBtn.Enabled   = editable;
        _linesGrid.ReadOnly       = !editable;
        _saveBtn.Enabled          = editable;
        _approveBtn.Enabled       = editable;
    }

    // ══════════════════════════════════════════════════════════
    // الأحداث
    // ══════════════════════════════════════════════════════════
    private void WireEvents()
    {
        _invoiceLoadBtn.Click += (_, _) => LoadInvoiceByNumber();
        _invoiceSearchBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) LoadInvoiceByNumber(); };
        _linesGrid.CellEndEdit   += OnCellEndEdit;
        _linesGrid.CellClick     += OnCellClick;
        _saveBtn.Click    += (_, _) => Save(approve: false);
        _approveBtn.Click += (_, _) => Save(approve: true);
    }

    private void LoadInvoiceByNumber()
    {
        var num = _invoiceSearchBox.Text.Trim();
        if (string.IsNullOrEmpty(num)) return;
        try
        {
            using var conn = DatabaseConnection.CreateConnection();
            const string sql = "SELECT id FROM public.purchase_invoices WHERE invoice_number=@num AND status='approved'";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("num", num);
            var result = cmd.ExecuteScalar();
            if (result is null) { ShowError("لم يُعثر على فاتورة معتمدة بهذا الرقم."); return; }

            int invId = (int)result;
            var inv   = new PurchaseRepository().GetById(invId);
            if (inv is null) { ShowError("تعذر تحميل الفاتورة."); return; }

            _linesGrid.Rows.Clear();
            _return.Lines.Clear();
            LoadInvoiceLines(inv);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void AddLineToGrid(PurchaseReturnLine l)
    {
        int idx = _linesGrid.Rows.Add(
            _linesGrid.Rows.Count + 1,
            l.ItemCode,
            l.ItemName,
            l.UnitName,
            l.MaxQty > 0 ? l.MaxQty.ToString("N3") : "—",
            l.Quantity.ToString("N3"),
            l.UnitPrice.ToString("N2"),
            (l.Quantity * l.UnitPrice).ToString("N2"),
            ""
        );
        // لون الصف
        _linesGrid.Rows[idx].DefaultCellStyle.BackColor =
            l.Quantity <= 0 ? ColorTranslator.FromHtml("#FFEBEE") : AppTheme.Surface;
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_linesGrid.Columns[e.ColumnIndex].Name != "colQty") return;

        var cell = _linesGrid.Rows[e.RowIndex].Cells["colQty"];
        if (!decimal.TryParse(cell.Value?.ToString(), out decimal newQty) || newQty < 0)
        {
            ShowError("الكمية يجب أن تكون صفراً أو أكبر.");
            cell.Value = _return.Lines[e.RowIndex].Quantity.ToString("N3");
            return;
        }

        var line = _return.Lines[e.RowIndex];
        if (line.MaxQty > 0 && newQty > line.MaxQty)
        {
            ShowError($"لا يمكن إرجاع أكثر من {line.MaxQty:N3}");
            cell.Value = line.MaxQty.ToString("N3");
            newQty     = line.MaxQty;
        }

        line.Quantity = newQty;
        _linesGrid.Rows[e.RowIndex].Cells["colTotal"].Value = (newQty * line.UnitPrice).ToString("N2");
        _linesGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor =
            newQty <= 0 ? ColorTranslator.FromHtml("#FFEBEE") : AppTheme.Surface;
        RecalcTotal();
        ShowInfo("تم تحديث الكمية.");
    }

    private void OnCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_linesGrid.Columns[e.ColumnIndex].Name != "colDel") return;
        if (_return.Status != "draft" && _return.Id > 0) return;

        _return.Lines.RemoveAt(e.RowIndex);
        _linesGrid.Rows.RemoveAt(e.RowIndex);
        for (int i = 0; i < _linesGrid.Rows.Count; i++)
            _linesGrid.Rows[i].Cells["colIdx"].Value = i + 1;
        RecalcTotal();
    }

    private void RecalcTotal()
    {
        decimal total = _return.Lines.Sum(l => l.Quantity * l.UnitPrice);
        _return.TotalAmount = total;
        _totalLbl.Text = total.ToString("N2");
    }

    // ══════════════════════════════════════════════════════════
    // حفظ / اعتماد
    // ══════════════════════════════════════════════════════════
    private void Save(bool approve)
    {
        // تحقق أساسي
        if (!ValidateForm()) return;

        try
        {
            // ملء بيانات الرأس
            _return.SupplierId  = ((ComboItem)_supplierCombo.SelectedItem!).Id;
            _return.WarehouseId = ((ComboItem)_warehouseCombo.SelectedItem!).Id;
            _return.ReturnDate  = _datePicker.Value.Date;
            _return.Notes       = _notesBox.Text.Trim();
            _return.CreatedBy   = supermarket.Services.SessionContext.CurrentUser?.Id;

            if (_sourceInvoice is not null)
            {
                _return.OriginalInvoiceId  = _sourceInvoice.Id;
                _return.OriginalInvoiceNum = _sourceInvoice.InvoiceNumber;
            }

            var repo = new PurchaseReturnRepository();
            int id   = repo.SaveDraft(_return);
            _return.Id = id;

            if (approve)
            {
                repo.Approve(id, _return.CreatedBy ?? 0);
                ShowSuccess("✅ تم اعتماد المرتجع بنجاح — تم خصم الكميات من المخزون.");
                SetFormEditable(false);
            }
            else
            {
                ShowSuccess("💾 تم حفظ المرتجع كمسودة.");
            }

            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            ShowError($"خطأ أثناء الحفظ: {ex.Message}");
        }
    }

    private bool ValidateForm()
    {
        if (_supplierCombo.SelectedIndex <= 0)
        { ShowError("يرجى اختيار المورد."); return false; }

        if (_warehouseCombo.SelectedIndex < 0)
        { ShowError("يرجى اختيار المستودع."); return false; }

        var activeLines = _return.Lines.Where(l => l.Quantity > 0).ToList();
        if (activeLines.Count == 0)
        { ShowError("يجب أن يحتوي المرتجع على صنف واحد على الأقل بكمية أكبر من صفر."); return false; }

        return true;
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════
    private static List<Warehouse> LoadWarehouses()
    {
        try
        {
            using var conn = DatabaseConnection.CreateConnection();
            const string sql = "SELECT id, name FROM public.warehouses WHERE is_active = true ORDER BY name";
            using var cmd = new NpgsqlCommand(sql, conn);
            using var r   = cmd.ExecuteReader();
            var list = new List<Warehouse>();
            while (r.Read())
                list.Add(new Warehouse { Id = r.GetInt32(0), Name = r.GetString(1) });
            return list;
        }
        catch { return new List<Warehouse>(); }
    }

    private static void SetComboById(ComboBox combo, int id)
    {
        for (int i = 0; i < combo.Items.Count; i++)
            if (combo.Items[i] is ComboItem ci && ci.Id == id)
            { combo.SelectedIndex = i; return; }
    }

    private void ShowSuccess(string m) { _feedbackLbl.ForeColor = AppTheme.Success;   _feedbackLbl.Text = m; }
    private void ShowError(string m)   { _feedbackLbl.ForeColor = AppTheme.Danger;    _feedbackLbl.Text = m; }
    private void ShowInfo(string m)    { _feedbackLbl.ForeColor = AppTheme.MutedText; _feedbackLbl.Text = m; }
}

// ── Helper classes (local to avoid conflicts) ─────────────────
file sealed class ComboItem
{
    public int    Id   { get; }
    public string Name { get; }
    public ComboItem(int id, string name) { Id = id; Name = name; }
    public override string ToString() => Name;
}
