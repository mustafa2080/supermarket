using System.Drawing;
using Npgsql;
using supermarket.Data;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-012 — نموذج إدخال/تعديل فاتورة الشراء
/// يدعم: إنشاء جديد، تعديل مسودة، اعتماد، إلغاء
/// </summary>
internal sealed class PurchaseInvoiceDialog : Form
{
    // ── حقول الرأس ──────────────────────────────────────────
    private readonly TextBox       _numBox;
    private readonly DateTimePicker _datePicker;
    private readonly ComboBox      _supplierCombo;
    private readonly ComboBox      _warehouseCombo;
    private readonly ComboBox      _paymentCombo;
    private readonly TextBox       _notesBox;

    // ── بحث الصنف ───────────────────────────────────────────
    private readonly TextBox       _itemSearchBox;
    private readonly Button        _itemSearchBtn;
    private readonly NumericUpDown _qtyInput;
    private readonly NumericUpDown _priceInput;
    private readonly NumericUpDown _discInput;
    private readonly NumericUpDown _taxInput;
    private readonly Button        _addLineBtn;

    // ── شبكة السطور ─────────────────────────────────────────
    private readonly DataGridView  _linesGrid;

    // ── الإجماليات ──────────────────────────────────────────
    private readonly Label _subtotalLbl;
    private readonly Label _discountLbl;
    private readonly Label _taxLbl;
    private readonly Label _netLbl;
    private readonly NumericUpDown _paidInput;
    private readonly Label _remainLbl;

    // ── أزرار الفاتورة ──────────────────────────────────────
    private readonly Button _saveDraftBtn;
    private readonly Button _approveBtn;
    private readonly Button _cancelInvBtn;
    private readonly Label  _feedbackLbl;

    // ── البيانات ─────────────────────────────────────────────
    private readonly PurchaseInvoice _invoice;
    private readonly List<Supplier>  _suppliers;
    private readonly List<Warehouse> _warehouses;
    private Item? _selectedItem;

    public PurchaseInvoiceDialog(PurchaseInvoice? existing = null)
    {
        _numBox        = AppTheme.CreateTextBox();
        _datePicker    = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _supplierCombo = AppTheme.CreateComboBox();
        _warehouseCombo= AppTheme.CreateComboBox();
        _paymentCombo  = AppTheme.CreateComboBox();
        _notesBox      = AppTheme.CreateTextBox("ملاحظات...");
        _itemSearchBox = AppTheme.CreateTextBox("ابحث بالاسم أو الباركود أو الكود...");
        _itemSearchBtn = new Button { Text = "🔍 بحث" };
        _qtyInput      = AppTheme.CreateNumericInput(3, 1) ;
        _priceInput    = AppTheme.CreateNumericInput(2, 0.01m);
        _discInput     = AppTheme.CreateNumericInput(2, 0.01m);
        _taxInput      = AppTheme.CreateNumericInput(2, 0.01m);
        _addLineBtn    = new Button { Text = "➕ إضافة سطر" };
        _linesGrid     = new DataGridView();
        _subtotalLbl   = new Label();
        _discountLbl   = new Label();
        _taxLbl        = new Label();
        _netLbl        = new Label();
        _paidInput     = AppTheme.CreateNumericInput(2, 1);
        _remainLbl     = new Label();
        _saveDraftBtn  = new Button { Text = "💾 حفظ مسودة" };
        _approveBtn    = new Button { Text = "✅ اعتماد" };
        _cancelInvBtn  = new Button { Text = "❌ إلغاء الفاتورة" };
        _feedbackLbl   = new Label();

        _invoice    = existing ?? new PurchaseInvoice();
        _suppliers  = new SupplierRepository().GetAll(activeOnly: true);
        _warehouses = LoadWarehouses();

        Text            = existing is null ? "فاتورة شراء جديدة" : $"فاتورة: {existing.InvoiceNumber}";
        Size            = new Size(1300, 820);
        MinimumSize     = new Size(1100, 700);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;
        RightToLeft     = RightToLeft.Yes;
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
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F)); // رأس الفاتورة
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));  // سطر الإضافة
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // الشبكة
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F)); // الإجماليات
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));  // شريط الأزرار

        root.Controls.Add(BuildHeaderPanel(),  0, 0);
        root.Controls.Add(BuildAddLinePanel(), 0, 1);
        root.Controls.Add(BuildLinesGrid(),    0, 2);
        root.Controls.Add(BuildTotalsPanel(),  0, 3);
        root.Controls.Add(BuildButtonBar(),    0, 4);
        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18, 16, 18, 14);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 4,
            RightToLeft = RightToLeft.Yes
        };
        for (int i = 0; i < 6; i++)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var headerLbl = new Label
        {
            Text = "بيانات فاتورة الشراء",
            Dock = DockStyle.Fill,
            Font = new Font("Tahoma", 15F, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };
        var subLbl = new Label
        {
            Text = "أدخل بيانات المورد والشراء بشكل منظم، ثم أضف السطور قبل الحفظ أو الاعتماد.",
            Dock = DockStyle.Fill,
            Font = new Font("Tahoma", 9F),
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleRight
        };
        tbl.Controls.Add(headerLbl, 0, 0);
        tbl.Controls.Add(subLbl, 0, 1);
        tbl.SetColumnSpan(headerLbl, 6);
        tbl.SetColumnSpan(subLbl, 6);

        // الرقم
        tbl.Controls.Add(AppTheme.CreateFieldLabel("رقم الفاتورة:"), 0, 2);
        _numBox.Dock = DockStyle.Fill; _numBox.ReadOnly = true;
        tbl.Controls.Add(_numBox, 0, 3);

        // التاريخ
        tbl.Controls.Add(AppTheme.CreateFieldLabel("التاريخ:"), 1, 2);
        _datePicker.Dock = DockStyle.Fill; _datePicker.Value = DateTime.Today;
        tbl.Controls.Add(_datePicker, 1, 3);

        // المورد
        tbl.Controls.Add(AppTheme.CreateFieldLabel("المورد: *"), 2, 2);
        _supplierCombo.Dock = DockStyle.Fill;
        tbl.Controls.Add(_supplierCombo, 2, 3);

        // المستودع
        tbl.Controls.Add(AppTheme.CreateFieldLabel("المستودع: *"), 3, 2);
        _warehouseCombo.Dock = DockStyle.Fill;
        tbl.Controls.Add(_warehouseCombo, 3, 3);

        // طريقة الدفع
        tbl.Controls.Add(AppTheme.CreateFieldLabel("طريقة الدفع:"), 4, 2);
        _paymentCombo.Dock = DockStyle.Fill;
        _paymentCombo.Items.AddRange(new object[] { "نقدي", "آجل", "شيك" });
        _paymentCombo.SelectedIndex = 0;
        tbl.Controls.Add(_paymentCombo, 4, 3);

        // ملاحظات
        tbl.Controls.Add(AppTheme.CreateFieldLabel("ملاحظات:"), 5, 2);
        _notesBox.Dock = DockStyle.Fill;
        tbl.Controls.Add(_notesBox, 5, 3);

        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildAddLinePanel()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(14, 12, 14, 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Text = "إضافة سطر إلى الفاتورة",
            Dock = DockStyle.Fill,
            Font = new Font("Tahoma", 12F, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        _itemSearchBox.Width = 300; _itemSearchBox.Margin = new Padding(4, 6, 4, 0);
        AppTheme.StyleSecondaryButton(_itemSearchBtn);
        _itemSearchBtn.Width = 90; _itemSearchBtn.Margin = new Padding(4, 4, 4, 0);

        var qtyLbl   = AppTheme.CreateFieldLabel("الكمية:"); qtyLbl.Margin = new Padding(10, 8, 4, 0);
        _qtyInput.Width = 80; _qtyInput.Minimum = 0.001m; _qtyInput.Value = 1; _qtyInput.Margin = new Padding(4, 4, 4, 0);

        var priceLbl = AppTheme.CreateFieldLabel("السعر:"); priceLbl.Margin = new Padding(10, 8, 4, 0);
        _priceInput.Width = 100; _priceInput.Margin = new Padding(4, 4, 4, 0);

        var discLbl  = AppTheme.CreateFieldLabel("الخصم:"); discLbl.Margin = new Padding(10, 8, 4, 0);
        _discInput.Width = 90; _discInput.Margin = new Padding(4, 4, 4, 0);

        var taxLbl   = AppTheme.CreateFieldLabel("ضريبة%:"); taxLbl.Margin = new Padding(10, 8, 4, 0);
        _taxInput.Width = 80; _taxInput.Margin = new Padding(4, 4, 4, 0);

        AppTheme.StylePrimaryButton(_addLineBtn);
        _addLineBtn.Width = 130; _addLineBtn.Margin = new Padding(4, 4, 4, 0);

        flow.Controls.Add(_addLineBtn);
        flow.Controls.Add(taxLbl);   flow.Controls.Add(_taxInput);
        flow.Controls.Add(discLbl);  flow.Controls.Add(_discInput);
        flow.Controls.Add(priceLbl); flow.Controls.Add(_priceInput);
        flow.Controls.Add(qtyLbl);   flow.Controls.Add(_qtyInput);
        flow.Controls.Add(_itemSearchBtn);
        flow.Controls.Add(_itemSearchBox);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(flow, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildLinesGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _linesGrid.Dock                  = DockStyle.Fill;
        _linesGrid.ReadOnly              = true;
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
        _linesGrid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _linesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _linesGrid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _linesGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _linesGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _linesGrid.EnableHeadersVisualStyles = false;
        _linesGrid.RowTemplate.Height = 36;

        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIdx",   HeaderText = "#",       Width = 40, FillWeight = 3 });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",  HeaderText = "الكود",   FillWeight = 8  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",  HeaderText = "الصنف",   FillWeight = 28 });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",  HeaderText = "الوحدة",  FillWeight = 8  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",   HeaderText = "الكمية",  FillWeight = 8  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "السعر",   FillWeight = 10 });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDisc",  HeaderText = "الخصم",   FillWeight = 8  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTax",   HeaderText = "ضريبة%",  FillWeight = 7  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTaxAmt",HeaderText = "الضريبة", FillWeight = 8  });
        _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي",FillWeight = 12 });

        // زر حذف السطر
        var delCol = new DataGridViewButtonColumn
        {
            Name = "colDel", HeaderText = "", Text = "🗑", UseColumnTextForButtonValue = true,
            Width = 40, FillWeight = 4
        };
        _linesGrid.Columns.Add(delCol);

        card.Controls.Add(_linesGrid);
        return card;
    }

    private Control BuildTotalsPanel()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(16, 14, 16, 10);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
            RightToLeft = RightToLeft.Yes
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));

        void AddTotalRow(int row, string labelText, Label valueLabel, Color color)
        {
            var lbl = AppTheme.CreateFieldLabel(labelText);
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.Font      = new Font("Tahoma", 11F, FontStyle.Bold);
            valueLabel.ForeColor = color;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.Dock      = DockStyle.Fill;
            valueLabel.Text      = "0.00";
            tbl.Controls.Add(lbl,        1, row);
            tbl.Controls.Add(valueLabel, 2, row);
        }

        AddTotalRow(0, "المجموع الفرعي:", _subtotalLbl, AppTheme.DarkText);
        AddTotalRow(1, "الخصم:",          _discountLbl, AppTheme.Warning);
        AddTotalRow(2, "الضريبة:",         _taxLbl,     AppTheme.MutedText);

        // صافي الإجمالي
        var netLblTitle = AppTheme.CreateFieldLabel("💰 صافي الإجمالي:");
        netLblTitle.TextAlign = ContentAlignment.MiddleLeft;
        netLblTitle.Font = new Font("Tahoma", 12F, FontStyle.Bold);
        _netLbl.Font = new Font("Tahoma", 14F, FontStyle.Bold);
        _netLbl.ForeColor = AppTheme.Primary;
        _netLbl.TextAlign = ContentAlignment.MiddleLeft;
        _netLbl.Dock = DockStyle.Fill;
        _netLbl.Text = "0.00";

        // المبلغ المدفوع
        var paidLbl = AppTheme.CreateFieldLabel("المدفوع:");
        paidLbl.TextAlign = ContentAlignment.MiddleLeft;
        _paidInput.Dock = DockStyle.Fill;
        _paidInput.Maximum = 99999999;

        // المتبقي
        _remainLbl.Font      = new Font("Tahoma", 11F, FontStyle.Bold);
        _remainLbl.ForeColor = AppTheme.Danger;
        _remainLbl.TextAlign = ContentAlignment.MiddleLeft;
        _remainLbl.Dock      = DockStyle.Fill;
        _remainLbl.Text      = "0.00";

        // فيدباك
        _feedbackLbl.Dock      = DockStyle.Fill;
        _feedbackLbl.Font      = AppTheme.BodyFont;
        _feedbackLbl.ForeColor = AppTheme.MutedText;
        _feedbackLbl.TextAlign = ContentAlignment.MiddleRight;

        tbl.SetRowSpan(_feedbackLbl, 3);
        tbl.Controls.Add(_feedbackLbl, 0, 0);

        // صف 0: صافي
        // نعيد ترتيب الصف الثالث ليكون صافي الإجمالي والمدفوع والمتبقي
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));

        card.Controls.Add(tbl);

        // نضيف باقي العناصر مباشرة فوق الكارد بـ FlowLayout
        var bottomFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 46,
            FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        bottomFlow.Controls.Add(AppTheme.CreateFieldLabel("المدفوع:"));
        _paidInput.Width = 130; _paidInput.Margin = new Padding(0, 4, 8, 0);
        bottomFlow.Controls.Add(_paidInput);
        bottomFlow.Controls.Add(AppTheme.CreateFieldLabel("المتبقي:"));
        _remainLbl.AutoSize = true; _remainLbl.Margin = new Padding(4, 8, 4, 0);
        bottomFlow.Controls.Add(_remainLbl);
        bottomFlow.Controls.Add(AppTheme.CreateFieldLabel("الصافي:"));
        _netLbl.AutoSize = true; _netLbl.Margin = new Padding(4, 8, 4, 0);
        bottomFlow.Controls.Add(_netLbl);

        card.Controls.Add(bottomFlow);
        return card;
    }

    private Control BuildButtonBar()
    {
        var wrap = AppTheme.CreateCard();
        wrap.Dock = DockStyle.Fill;
        wrap.Padding = new Padding(12, 8, 12, 8);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 4, 0, 0)
        };

        AppTheme.StylePrimaryButton(_saveDraftBtn);  _saveDraftBtn.Width = 160; _saveDraftBtn.Margin = new Padding(6, 0, 0, 0);
        AppTheme.StylePrimaryButton(_approveBtn);    _approveBtn.Width   = 160; _approveBtn.Margin   = new Padding(6, 0, 0, 0);
        _approveBtn.BackColor = AppTheme.Success;
        AppTheme.StyleSecondaryButton(_cancelInvBtn);_cancelInvBtn.Width = 160; _cancelInvBtn.Margin = new Padding(6, 0, 0, 0);
        _cancelInvBtn.ForeColor = AppTheme.Danger;

        _feedbackLbl.Dock      = DockStyle.Fill;
        _feedbackLbl.Font      = AppTheme.BodyFont;
        _feedbackLbl.ForeColor = AppTheme.MutedText;
        _feedbackLbl.TextAlign = ContentAlignment.MiddleRight;

        bar.Controls.Add(_cancelInvBtn);
        bar.Controls.Add(_approveBtn);
        bar.Controls.Add(_saveDraftBtn);
        bar.Controls.Add(_feedbackLbl);
        wrap.Controls.Add(bar);
        return wrap;
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

        // رقم الفاتورة
        if (_invoice.Id == 0)
        {
            _numBox.Text = new PurchaseRepository().NextInvoiceNumber();
            _invoice.InvoiceNumber = _numBox.Text;
        }
        else
        {
            _numBox.Text      = _invoice.InvoiceNumber;
            _datePicker.Value = _invoice.InvoiceDate;
            SetComboById(_supplierCombo,  _invoice.SupplierId);
            SetComboById(_warehouseCombo, _invoice.WarehouseId);
            _paymentCombo.SelectedIndex = _invoice.PaymentMethod switch
            {
                "credit" => 1, "check" => 2, _ => 0
            };
            _notesBox.Text = _invoice.Notes;
            _paidInput.Value = _invoice.PaidAmount;
            // تحميل السطور
            foreach (var l in _invoice.Lines)
                AddLineToGrid(l);
            RecalcTotals();
        }

        // تعطيل التعديل إذا مش مسودة
        bool editable = _invoice.Id == 0 || _invoice.Status == "draft";
        SetFormEditable(editable);
    }

    private void SetFormEditable(bool editable)
    {
        _supplierCombo.Enabled  = editable;
        _warehouseCombo.Enabled = editable;
        _paymentCombo.Enabled   = editable;
        _datePicker.Enabled     = editable;
        _notesBox.ReadOnly      = !editable;
        _itemSearchBox.Enabled  = editable;
        _itemSearchBtn.Enabled  = editable;
        _qtyInput.Enabled       = editable;
        _priceInput.Enabled     = editable;
        _discInput.Enabled      = editable;
        _taxInput.Enabled       = editable;
        _addLineBtn.Enabled     = editable;
        _saveDraftBtn.Enabled   = editable;
        _approveBtn.Enabled     = editable;
        _paidInput.Enabled      = editable;
        _cancelInvBtn.Enabled   = editable && _invoice.Id > 0;
    }

    // ══════════════════════════════════════════════════════════
    // الأحداث
    // ══════════════════════════════════════════════════════════
    private void WireEvents()
    {
        _itemSearchBox.KeyDown   += (_, e) => { if (e.KeyCode == Keys.Enter) SearchItem(); };
        _itemSearchBtn.Click     += (_, _) => SearchItem();
        _addLineBtn.Click        += (_, _) => AddLine();
        _paidInput.ValueChanged  += (_, _) => RecalcTotals();
        _linesGrid.CellClick     += OnGridCellClick;
        _saveDraftBtn.Click      += (_, _) => Save(approve: false);
        _approveBtn.Click        += (_, _) => Save(approve: true);
        _cancelInvBtn.Click      += (_, _) => CancelInvoice();
    }

    private void SearchItem()
    {
        var q = _itemSearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q)) return;

        try
        {
            var repo  = new ItemRepository();
            var items = repo.Search(q, groupId: null);
            if (items.Count == 0)   { ShowError("لم يُعثر على صنف."); return; }
            if (items.Count == 1)   { SelectItem(items[0]); return; }

            // اختيار من قائمة
            using var picker = new ItemPickerDialog(items);
            if (picker.ShowDialog() == DialogResult.OK && picker.SelectedItem is not null)
                SelectItem(picker.SelectedItem);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void SelectItem(Item item)
    {
        _selectedItem  = item;
        _itemSearchBox.Text  = $"{item.ItemCode} — {item.NameAr}";
        _priceInput.Value    = item.PurchasePrice > 0 ? item.PurchasePrice : 0;
        _taxInput.Value      = item.TaxRate;
        _qtyInput.Value      = 1;
        _discInput.Value     = 0;
        ShowInfo($"تم اختيار: {item.NameAr}");
    }

    private void AddLine()
    {
        if (_selectedItem is null) { ShowError("اختر صنفاً أولاً."); return; }
        if (_qtyInput.Value <= 0)  { ShowError("الكمية يجب أن تكون أكبر من صفر."); return; }
        if (_priceInput.Value <= 0){ ShowError("السعر يجب أن يكون أكبر من صفر."); return; }

        var line = new PurchaseInvoiceLine
        {
            ItemId    = _selectedItem.Id,
            ItemCode  = _selectedItem.ItemCode,
            ItemName  = _selectedItem.NameAr,
            UnitName  = _selectedItem.UnitName,
            Quantity  = _qtyInput.Value,
            UnitPrice = _priceInput.Value,
            Discount  = _discInput.Value,
            TaxRate   = _taxInput.Value
        };
        line.RecalcTotal();

        _invoice.Lines.Add(line);
        AddLineToGrid(line);
        RecalcTotals();

        // تصفير حقول الإضافة
        _selectedItem    = null;
        _itemSearchBox.Text = string.Empty;
        _priceInput.Value   = 0;
        _discInput.Value    = 0;
        _taxInput.Value     = 0;
        _qtyInput.Value     = 1;
        _itemSearchBox.Focus();
    }

    private void AddLineToGrid(PurchaseInvoiceLine l)
    {
        int idx = _linesGrid.Rows.Add(
            _linesGrid.Rows.Count + 1,
            l.ItemCode,
            l.ItemName,
            l.UnitName,
            l.Quantity.ToString("N3"),
            l.UnitPrice.ToString("N2"),
            l.Discount > 0 ? l.Discount.ToString("N2") : "—",
            l.TaxRate > 0  ? l.TaxRate.ToString("N1") + "%" : "—",
            l.TaxAmount > 0? l.TaxAmount.ToString("N2") : "—",
            l.LineTotal.ToString("N2"),
            ""
        );
    }

    private void OnGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_linesGrid.Columns[e.ColumnIndex].Name == "colDel")
        {
            bool editable = _invoice.Id == 0 || _invoice.Status == "draft";
            if (!editable) return;
            _invoice.Lines.RemoveAt(e.RowIndex);
            _linesGrid.Rows.RemoveAt(e.RowIndex);
            // إعادة ترقيم
            for (int i = 0; i < _linesGrid.Rows.Count; i++)
                _linesGrid.Rows[i].Cells["colIdx"].Value = i + 1;
            RecalcTotals();
        }
    }

    private void RecalcTotals()
    {
        decimal sub  = _invoice.Lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal disc = _invoice.Lines.Sum(l => l.Discount);
        decimal tax  = _invoice.Lines.Sum(l => l.TaxAmount);
        decimal net  = sub - disc + tax;
        decimal paid = _paidInput.Value;
        decimal rem  = net - paid;

        _subtotalLbl.Text = sub.ToString("N2");
        _discountLbl.Text = disc.ToString("N2");
        _taxLbl.Text      = tax.ToString("N2");
        _netLbl.Text      = net.ToString("N2");
        _remainLbl.Text   = rem.ToString("N2");
        _remainLbl.ForeColor = rem > 0 ? AppTheme.Danger : AppTheme.Success;

        _invoice.Subtotal   = sub;
        _invoice.Discount   = disc;
        _invoice.TaxAmount  = tax;
        _invoice.NetTotal   = net;
        _invoice.PaidAmount = paid;
        _invoice.Remaining  = rem;
    }

    // ══════════════════════════════════════════════════════════
    // الحفظ والاعتماد
    // ══════════════════════════════════════════════════════════
    private void Save(bool approve)
    {
        if (!Validate()) return;

        CollectHeaderData();

        try
        {
            var repo = new PurchaseRepository();
            int id   = repo.SaveDraft(_invoice);
            _invoice.Id = id;
            AuditService.LogCreate("public.purchase_invoices", id);

            if (approve)
            {
                repo.Approve(id, SessionContext.CurrentUser!.Id);
                AuditService.LogUpdate("public.purchase_invoices", id);
                ShowSuccess($"✅ تم اعتماد الفاتورة {_invoice.InvoiceNumber} بنجاح — المخزون مُحدَّث.");
                SetFormEditable(false);
            }
            else
            {
                ShowSuccess($"💾 تم حفظ المسودة {_invoice.InvoiceNumber}.");
            }

            DialogResult = approve ? DialogResult.OK : DialogResult.None;
        }
        catch (Exception ex) { ShowError($"خطأ أثناء الحفظ: {ex.Message}"); }
    }

    private void CancelInvoice()
    {
        if (_invoice.Id == 0) { Close(); return; }
        if (_invoice.Status != "draft") { ShowError("لا يمكن إلغاء فاتورة معتمدة."); return; }

        var confirm = MessageBox.Show(
            "هل تريد إلغاء هذه الفاتورة؟ لا يمكن التراجع.",
            "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            new PurchaseRepository().Cancel(_invoice.Id);
            AuditService.LogDelete("public.purchase_invoices", _invoice.Id);
            ShowSuccess("تم إلغاء الفاتورة.");
            SetFormEditable(false);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private new bool Validate()
    {
        if (_supplierCombo.SelectedIndex <= 0)
            { ShowError("اختر المورد."); return false; }
        if (_warehouseCombo.SelectedIndex < 0)
            { ShowError("اختر المستودع."); return false; }
        if (_invoice.Lines.Count == 0)
            { ShowError("أضف سطراً واحداً على الأقل."); return false; }
        return true;
    }

    private void CollectHeaderData()
    {
        _invoice.InvoiceDate   = _datePicker.Value.Date;
        _invoice.SupplierId    = ((ComboItem)_supplierCombo.SelectedItem!).Id;
        _invoice.WarehouseId   = ((ComboItem)_warehouseCombo.SelectedItem!).Id;
        _invoice.PaymentMethod = _paymentCombo.SelectedIndex switch { 1 => "credit", 2 => "check", _ => "cash" };
        _invoice.Notes         = _notesBox.Text.Trim();
        _invoice.CreatedBy     = SessionContext.CurrentUser?.Id;
        RecalcTotals();
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════
    private static List<Warehouse> LoadWarehouses()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = "SELECT id, name, COALESCE(location,''), is_default, is_active FROM public.warehouses WHERE is_active = TRUE ORDER BY is_default DESC, name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var list = new List<Warehouse>();
        while (r.Read()) list.Add(new Warehouse
        {
            Id = r.GetInt32(0), Name = r.GetString(1), Location = r.GetString(2),
            IsDefault = r.GetBoolean(3), IsActive = r.GetBoolean(4)
        });
        return list;
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

    private record ComboItem(int Id, string Name) { public override string ToString() => Name; }
}

// ── حوار اختيار صنف من قائمة نتائج البحث ────────────────────
internal sealed class ItemPickerDialog : Form
{
    public Item? SelectedItem { get; private set; }

    public ItemPickerDialog(List<Item> items)
    {
        Text = "اختر صنفاً"; Size = new Size(640, 400);
        StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",  HeaderText = "الكود",   FillWeight = 15 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",  HeaderText = "الاسم",   FillWeight = 45 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGroup", HeaderText = "المجموعة",FillWeight = 20 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "سعر الشراء",FillWeight = 20 });

        foreach (var item in items)
            grid.Rows.Add(item.ItemCode, item.NameAr, item.GroupName, item.PurchasePrice.ToString("N2"));

        grid.Tag = items;
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            SelectedItem = items[e.RowIndex];
            DialogResult = DialogResult.OK;
        };

        var selectBtn = new Button { Text = "اختر", Dock = DockStyle.Bottom, Height = 38 };
        AppTheme.StylePrimaryButton(selectBtn);
        selectBtn.Click += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            SelectedItem = items[grid.SelectedRows[0].Index];
            DialogResult = DialogResult.OK;
        };

        Controls.Add(grid);
        Controls.Add(selectBtn);
    }
}
