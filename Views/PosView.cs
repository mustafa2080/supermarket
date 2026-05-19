using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-014 — شاشة نقطة البيع (POS)
/// F1=بحث | F2=دفع/طباعة | F3=تعليق | Del=حذف | Enter=إضافة
/// </summary>
internal sealed class PosView : UserControl
{
    // ── بيانات الجلسة ────────────────────────────────────────
    private readonly SalesRepository _repo   = new();
    private Warehouse  _warehouse            = new() { Id = 1, Name = "الرئيسي" };
    private List<Customer> _customers        = new();
    private readonly List<HoldInvoice> _held = new();

    // ── بيانات الفاتورة الحالية ──────────────────────────────
    private readonly List<SalesInvoiceLine> _lines = new();
    private int?    _customerId;
    private string  _customerName  = string.Empty;
    private string  _payMethod     = "cash";
    private decimal _invoiceDiscount;

    // ── Controls ─────────────────────────────────────────────
    private readonly TextBox        _searchBox;
    private readonly ListBox        _searchList;
    private readonly DataGridView   _cartGrid;
    private readonly ComboBox       _customerCombo;
    private readonly Label          _lblSubtotal, _lblDiscount, _lblTax, _lblNet;
    private readonly TextBox        _txtInvDiscount, _txtPaid;
    private readonly Label          _lblChange;
    private readonly Button         _btnCash, _btnVisa, _btnCredit;
    private readonly Button         _btnPay, _btnHold, _btnResume;
    private readonly Label          _lblFeedback;
    private readonly Label          _lblInvNum, _lblCashier, _lblTime;
    private readonly Panel          _payPanel;

    public PosView()
    {
        _searchBox      = new TextBox();
        _searchList     = new ListBox();
        _cartGrid       = new DataGridView();
        _customerCombo  = AppTheme.CreateComboBox();
        _lblSubtotal    = new Label();
        _lblDiscount    = new Label();
        _lblTax         = new Label();
        _lblNet         = new Label();
        _txtInvDiscount = new TextBox();
        _txtPaid        = new TextBox();
        _lblChange      = new Label();
        _btnCash        = new Button { Text = "💵 نقدي",  Tag = "cash"   };
        _btnVisa        = new Button { Text = "💳 فيزا",  Tag = "visa"   };
        _btnCredit      = new Button { Text = "📋 آجل",   Tag = "credit" };
        _btnPay         = new Button { Text = "🖨 دفع وطباعة  F2" };
        _btnHold        = new Button { Text = "⏸ تعليق  F3" };
        _btnResume      = new Button { Text = "▶ استئناف معلقة" };
        _lblFeedback    = new Label();
        _lblInvNum      = new Label();
        _lblCashier     = new Label();
        _lblTime        = new Label();
        _payPanel       = new Panel();

        Dock        = DockStyle.Fill;
        BackColor   = Color.FromArgb(30, 30, 46);
        RightToLeft = RightToLeft.Yes;

        LoadSessionData();
        BuildUI();
        WireEvents();
        NewInvoice();

        // تحديث الساعة كل ثانية
        var clock = new System.Windows.Forms.Timer { Interval = 1000 };
        clock.Tick += (_, _) => _lblTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
        clock.Start();
    }

    // ══════════════════════════════════════════════════════════
    // تحميل بيانات الجلسة
    // ══════════════════════════════════════════════════════════
    private void LoadSessionData()
    {
        try
        {
            _warehouse = _repo.GetDefaultWarehouse() ?? _warehouse;
            _customers = _repo.GetCustomers();
        }
        catch { /* يعمل بدون DB في وضع التصميم */ }
    }

    // ══════════════════════════════════════════════════════════
    // بناء الواجهة
    // ══════════════════════════════════════════════════════════
    private void BuildUI()
    {
        // شريط العنوان العلوي
        var topBar = BuildTopBar();

        // اللوح الأيمن: بحث + سلة
        var leftPanel = BuildLeftPanel();

        // اللوح الأيسر: إجماليات + دفع
        var rightPanel = BuildRightPanel();

        var split = new SplitContainer
        {
            Dock            = DockStyle.Fill,
            Orientation     = Orientation.Vertical,
            SplitterWidth   = 6,
            SplitterDistance = 700,
            BackColor       = Color.FromArgb(30, 30, 46),
            Panel1MinSize   = 400,
            Panel2MinSize   = 280
        };
        split.Panel1.Controls.Add(leftPanel);
        split.Panel2.Controls.Add(rightPanel);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Color.FromArgb(30, 30, 46)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(split,  0, 1);
        Controls.Add(root);
    }

    // ── شريط العنوان ─────────────────────────────────────────
    private Control BuildTopBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 22, 35),
            Padding   = new Padding(12, 8, 12, 8)
        };

        _lblInvNum.AutoSize  = true;
        _lblInvNum.Font      = new Font("Tahoma", 11F, FontStyle.Bold);
        _lblInvNum.ForeColor = AppTheme.Accent;
        _lblInvNum.Location  = new Point(12, 14);

        _lblCashier.AutoSize  = true;
        _lblCashier.Font      = AppTheme.BodyFont;
        _lblCashier.ForeColor = Color.Silver;
        _lblCashier.Location  = new Point(250, 16);
        _lblCashier.Text      = $"الكاشير: {SessionContext.DisplayName}  |  المستودع: {_warehouse.Name}";

        _lblTime.AutoSize  = true;
        _lblTime.Font      = new Font("Tahoma", 13F, FontStyle.Bold);
        _lblTime.ForeColor = Color.LightGreen;
        _lblTime.Text      = DateTime.Now.ToString("hh:mm:ss tt");
        _lblTime.Anchor    = AnchorStyles.Left | AnchorStyles.Top;
        _lblTime.Location  = new Point(bar.Width - 160, 12);

        _lblFeedback.AutoSize  = false;
        _lblFeedback.Dock      = DockStyle.Bottom;
        _lblFeedback.Height    = 0; // مخفي افتراضياً
        _lblFeedback.Font      = AppTheme.BodyFont;
        _lblFeedback.TextAlign = ContentAlignment.MiddleRight;
        _lblFeedback.ForeColor = AppTheme.Success;

        bar.Controls.Add(_lblInvNum);
        bar.Controls.Add(_lblCashier);
        bar.Controls.Add(_lblTime);
        return bar;
    }

    // ── اللوح الأيمن: بحث + عميل + سلة ─────────────────────
    private Control BuildLeftPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 46), Padding = new Padding(6) };

        // صف البحث
        var searchRow = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
        var lblSearch = new Label { Text = "🔍 بحث (F1):", AutoSize = true, ForeColor = Color.Silver,
            Font = AppTheme.BodyFont, Location = new Point(0, 12) };
        _searchBox.Location    = new Point(105, 8);
        _searchBox.Width       = 340;
        _searchBox.Font        = new Font("Tahoma", 12F);
        _searchBox.BackColor   = Color.FromArgb(50, 50, 70);
        _searchBox.ForeColor   = Color.White;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.PlaceholderText = "اسم الصنف أو الباركود أو الكود...";
        searchRow.Controls.Add(_searchBox);
        searchRow.Controls.Add(lblSearch);

        // قائمة نتائج البحث (Dropdown)
        _searchList.Dock             = DockStyle.Top;
        _searchList.Height           = 0; // مخفية في البداية
        _searchList.BackColor        = Color.FromArgb(45, 45, 65);
        _searchList.ForeColor        = Color.White;
        _searchList.Font             = new Font("Tahoma", 10F);
        _searchList.BorderStyle      = BorderStyle.FixedSingle;
        _searchList.ItemHeight       = 28;
        _searchList.IntegralHeight   = false;

        // صف العميل
        var customerRow = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        var lblCust = new Label { Text = "👤 العميل:", AutoSize = true, ForeColor = Color.Silver,
            Font = AppTheme.BodyFont, Location = new Point(0, 10) };
        _customerCombo.Location     = new Point(80, 6);
        _customerCombo.Width        = 300;
        _customerCombo.BackColor    = Color.FromArgb(50, 50, 70);
        _customerCombo.ForeColor    = Color.White;
        _customerCombo.FlatStyle    = FlatStyle.Flat;
        customerRow.Controls.Add(_customerCombo);
        customerRow.Controls.Add(lblCust);

        // السلة
        BuildCartGrid();
        _cartGrid.Dock = DockStyle.Fill;

        // شريط feedback
        _lblFeedback.Dock      = DockStyle.Bottom;
        _lblFeedback.Height    = 28;
        _lblFeedback.Font      = AppTheme.BodyFont;
        _lblFeedback.TextAlign = ContentAlignment.MiddleRight;
        _lblFeedback.BackColor = Color.FromArgb(22, 22, 35);
        _lblFeedback.ForeColor = AppTheme.Success;

        p.Controls.Add(_cartGrid);
        p.Controls.Add(_searchList);
        p.Controls.Add(customerRow);
        p.Controls.Add(searchRow);
        p.Controls.Add(_lblFeedback);
        return p;
    }

    private void BuildCartGrid()
    {
        _cartGrid.AllowUserToAddRows    = false;
        _cartGrid.AllowUserToDeleteRows = false;
        _cartGrid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _cartGrid.MultiSelect           = false;
        _cartGrid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _cartGrid.RowHeadersVisible     = false;
        _cartGrid.BorderStyle           = BorderStyle.None;
        _cartGrid.Font                  = new Font("Tahoma", 10F);
        _cartGrid.BackgroundColor       = Color.FromArgb(38, 38, 55);
        _cartGrid.GridColor             = Color.FromArgb(60, 60, 80);
        _cartGrid.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 55);
        _cartGrid.DefaultCellStyle.ForeColor = Color.White;
        _cartGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 100, 160);
        _cartGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(43, 43, 62);
        _cartGrid.ColumnHeadersDefaultCellStyle.BackColor   = Color.FromArgb(22, 22, 35);
        _cartGrid.ColumnHeadersDefaultCellStyle.ForeColor   = AppTheme.Accent;
        _cartGrid.ColumnHeadersDefaultCellStyle.Font        = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _cartGrid.EnableHeadersVisualStyles = false;
        _cartGrid.RowTemplate.Height        = 38;

        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIdx",   HeaderText = "#",         Width = 38,  FillWeight = 3,  ReadOnly = true });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",  HeaderText = "الصنف",     FillWeight = 38, ReadOnly = true });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",  HeaderText = "الوحدة",    FillWeight = 9,  ReadOnly = true });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",   HeaderText = "الكمية",    FillWeight = 10 }); // قابل للتعديل
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "السعر",     FillWeight = 12, ReadOnly = true });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDisc",  HeaderText = "خصم",       FillWeight = 9  }); // قابل للتعديل
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "الإجمالي",  FillWeight = 14, ReadOnly = true });

        // زر حذف
        _cartGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "colDel", HeaderText = "", Text = "🗑", UseColumnTextForButtonValue = true,
            Width = 38, FillWeight = 5
        });

        // تمييز عمود الكمية والخصم
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.BackColor  = Color.FromArgb(45, 75, 45);
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.ForeColor  = Color.LightGreen;
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.Font       = new Font("Tahoma", 11F, FontStyle.Bold);
        _cartGrid.Columns["colDisc"]!.DefaultCellStyle.BackColor = Color.FromArgb(75, 55, 35);
        _cartGrid.Columns["colDisc"]!.DefaultCellStyle.ForeColor = Color.Orange;
    }

    // ══════════════════════════════════════════════════════════
    // اللوح الأيمن: الإجماليات + الدفع + الأزرار
    // ══════════════════════════════════════════════════════════
    private Control BuildRightPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 22, 35), Padding = new Padding(8) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(BuildTotalsPanel(),    0, 0);
        layout.Controls.Add(BuildPayMethodPanel(), 0, 1);
        layout.Controls.Add(BuildPaidPanel(),      0, 2);
        layout.Controls.Add(BuildActionPanel(),    0, 3);
        p.Controls.Add(layout);
        return p;
    }

    private Control BuildTotalsPanel()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(38, 38, 55), Padding = new Padding(10) };
        _lblSubtotal.Text = "0.00"; _lblSubtotal.AutoSize = true; _lblSubtotal.ForeColor = Color.White;      _lblSubtotal.Font = AppTheme.BodyFont;
        _lblDiscount.Text = "0.00"; _lblDiscount.AutoSize = true; _lblDiscount.ForeColor = Color.Orange;     _lblDiscount.Font = AppTheme.BodyFont;
        _lblTax.Text      = "0.00"; _lblTax.AutoSize      = true; _lblTax.ForeColor      = Color.LightBlue;  _lblTax.Font      = AppTheme.BodyFont;
        _lblNet.Text      = "0.00"; _lblNet.AutoSize      = true; _lblNet.ForeColor      = Color.Gold;        _lblNet.Font      = new Font("Tahoma", 14F, FontStyle.Bold);
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        for (int i = 0; i < 4; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        Label Lbl(string t, bool big = false) => new Label { Text = t, AutoSize = true, ForeColor = big ? Color.Gold : Color.Silver, Font = big ? new Font("Tahoma", 13F, FontStyle.Bold) : AppTheme.BodyFont };
        tbl.Controls.Add(Lbl("المجموع الفرعي:"),       0, 0); tbl.Controls.Add(_lblSubtotal, 1, 0);
        tbl.Controls.Add(Lbl("الخصم:"),                0, 1); tbl.Controls.Add(_lblDiscount, 1, 1);
        tbl.Controls.Add(Lbl("الضريبة:"),              0, 2); tbl.Controls.Add(_lblTax,      1, 2);
        tbl.Controls.Add(Lbl("الصافي:", big: true),    0, 3); tbl.Controls.Add(_lblNet,      1, 3);
        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildPayMethodPanel()
    {
        var p    = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        void StylePayBtn(Button b, Color col)
        {
            b.Width = 85; b.Height = 38; b.Font = new Font("Tahoma", 10F, FontStyle.Bold);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = col; b.ForeColor = Color.White;
            b.Margin = new Padding(4, 0, 4, 0);
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.BorderColor = Color.Gold;
        }
        StylePayBtn(_btnCash,   Color.FromArgb(34, 120, 60));
        StylePayBtn(_btnVisa,   Color.FromArgb(30, 80, 160));
        StylePayBtn(_btnCredit, Color.FromArgb(140, 60, 20));
        flow.Controls.Add(_btnCash); flow.Controls.Add(_btnVisa); flow.Controls.Add(_btnCredit);
        p.Controls.Add(flow);
        return p;
    }

    private Control BuildPaidPanel()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(38, 38, 55), Padding = new Padding(8, 6, 8, 6) };
        var tbl  = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        for (int i = 0; i < 3; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        _txtInvDiscount.Width = 100; _txtInvDiscount.BackColor = Color.FromArgb(50, 50, 70); _txtInvDiscount.ForeColor = Color.Orange;
        _txtInvDiscount.Font = AppTheme.BodyFont; _txtInvDiscount.BorderStyle = BorderStyle.FixedSingle; _txtInvDiscount.Text = "0";
        _txtPaid.Width = 120; _txtPaid.BackColor = Color.FromArgb(30, 60, 30); _txtPaid.ForeColor = Color.LightGreen;
        _txtPaid.Font  = new Font("Tahoma", 12F, FontStyle.Bold); _txtPaid.BorderStyle = BorderStyle.FixedSingle;
        _lblChange.AutoSize = true; _lblChange.Text = "0.00"; _lblChange.ForeColor = Color.Gold;
        _lblChange.Font = new Font("Tahoma", 13F, FontStyle.Bold);
        Label Lbl(string t, Color c) => new Label { Text = t, AutoSize = true, ForeColor = c, Font = new Font("Tahoma", 10F, FontStyle.Bold), Margin = new Padding(0, 6, 4, 0) };
        tbl.Controls.Add(Lbl("خصم الفاتورة:", Color.Orange),     0, 0); tbl.Controls.Add(_txtInvDiscount, 1, 0);
        tbl.Controls.Add(Lbl("المدفوع:",       Color.LightGreen), 0, 1); tbl.Controls.Add(_txtPaid,        1, 1);
        tbl.Controls.Add(Lbl("الباقي:",        Color.Gold),       0, 2); tbl.Controls.Add(_lblChange,      1, 2);
        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildActionPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        void StyleBtn(Button b, Color col, int h = 48)
        {
            b.Dock = DockStyle.Top; b.Height = h; b.Font = new Font("Tahoma", 11F, FontStyle.Bold);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = col; b.ForeColor = Color.White;
            b.Margin = new Padding(0, 0, 0, 6); b.FlatAppearance.BorderSize = 0;
        }
        StyleBtn(_btnPay,    Color.FromArgb(34, 130, 60));
        StyleBtn(_btnHold,   Color.FromArgb(100, 80, 30));
        StyleBtn(_btnResume, Color.FromArgb(40, 80, 130), 38);
        // Dock.Top: المضاف أولاً يظهر في الأسفل، لذا نعكس الترتيب
        p.Controls.Add(_btnResume);
        p.Controls.Add(_btnHold);
        p.Controls.Add(_btnPay);
        return p;
    }

    // ══════════════════════════════════════════════════════════
    // ربط الأحداث
    // ══════════════════════════════════════════════════════════
    private void WireEvents()
    {
        // بحث
        _searchBox.TextChanged  += SearchBox_TextChanged;
        _searchBox.KeyDown      += SearchBox_KeyDown;
        _searchList.DoubleClick += (_, _) => AddSelectedFromList();
        _searchList.KeyDown     += (_, e) => { if (e.KeyCode == Keys.Enter) AddSelectedFromList(); };

        // السلة
        _cartGrid.CellEndEdit += CartGrid_CellEndEdit;
        _cartGrid.CellClick   += CartGrid_CellClick;
        _cartGrid.KeyDown     += CartGrid_KeyDown;

        // العميل
        _customerCombo.SelectedIndexChanged += CustomerCombo_Changed;

        // طريقة الدفع
        _btnCash.Click   += (_, _) => SetPayMethod("cash");
        _btnVisa.Click   += (_, _) => SetPayMethod("visa");
        _btnCredit.Click += (_, _) => SetPayMethod("credit");

        // المدفوع
        _txtPaid.TextChanged      += (_, _) => RecalcChange();
        _txtInvDiscount.TextChanged += (_, _) => RecalcTotals();

        // أزرار رئيسية
        _btnPay.Click    += (_, _) => ProcessPayment();
        _btnHold.Click   += (_, _) => HoldInvoice();
        _btnResume.Click += (_, _) => ResumeInvoice();

        // Keyboard shortcuts (F1/F2/F3/Del)
        // KeyPreview متاح فقط على Form، نضبطه عند تحميل الـ UserControl
        this.Load    += (_, _) => { if (ParentForm is not null) { ParentForm.KeyPreview = true; ParentForm.KeyDown += PosView_KeyDown; } };
        this.KeyDown += PosView_KeyDown;
    }

    private void PosView_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F1: _searchBox.Focus(); e.Handled = true; break;
            case Keys.F2: ProcessPayment();   e.Handled = true; break;
            case Keys.F3: HoldInvoice();      e.Handled = true; break;
            case Keys.Delete when _cartGrid.Focused:
                DeleteSelectedLine(); e.Handled = true; break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // فاتورة جديدة
    // ══════════════════════════════════════════════════════════
    private void NewInvoice()
    {
        _lines.Clear();
        _cartGrid.Rows.Clear();
        _customerId      = null;
        _customerName    = string.Empty;
        _payMethod       = "cash";
        _invoiceDiscount = 0;
        _txtInvDiscount.Text = "0";
        _txtPaid.Text        = "";
        _lblChange.Text      = "0.00";

        // تحميل العملاء في الكومبو
        _customerCombo.Items.Clear();
        _customerCombo.Items.Add(new ComboItem(0, "-- زبون عادي (نقدي) --"));
        foreach (var c in _customers)
            _customerCombo.Items.Add(new ComboItem(c.Id, $"{c.Name}  ({c.LoyaltyPoints:N0} نقطة)"));
        _customerCombo.SelectedIndex = 0;

        SetPayMethod("cash");
        RecalcTotals();

        try { _lblInvNum.Text = $"🧾 فاتورة:  {_repo.NextInvoiceNumber()}"; }
        catch { _lblInvNum.Text = "🧾 فاتورة جديدة"; }

        _searchBox.Clear();
        _searchBox.Focus();
        ShowInfo("فاتورة جديدة — ابدأ بمسح الباركود أو ابحث بـ F1.");
    }

    // ══════════════════════════════════════════════════════════
    // البحث عن الأصناف
    // ══════════════════════════════════════════════════════════
    private System.Windows.Forms.Timer? _searchTimer;
    private readonly List<Item> _searchResults = new();

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        var q = _searchBox.Text.Trim();

        // باركود مكتمل (13 رقم) → إضافة فورية
        if (q.Length >= 8 && q.All(char.IsDigit))
        {
            _searchTimer?.Stop();
            AddByBarcode(q);
            return;
        }

        if (q.Length < 2) { HideSearchList(); return; }

        // تأخير 300ms قبل البحث
        _searchTimer?.Stop();
        _searchTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); DoSearch(q); };
        _searchTimer.Start();
    }

    private void DoSearch(string q)
    {
        try
        {
            _searchResults.Clear();
            _searchResults.AddRange(_repo.SearchItems(q, _warehouse.Id));
            _searchList.Items.Clear();

            if (_searchResults.Count == 0) { HideSearchList(); ShowInfo("لا توجد نتائج."); return; }

            foreach (var item in _searchResults)
                _searchList.Items.Add($"{item.NameAr}  |  {item.RetailPrice:N2} ج.م  |  رصيد: {item.CurrentStock:N0}");

            _searchList.Height = Math.Min(_searchResults.Count * 28, 196);

            if (_searchResults.Count == 1)
            {
                AddItemToCart(_searchResults[0]);
                HideSearchList();
                _searchBox.Clear();
            }
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void AddByBarcode(string barcode)
    {
        try
        {
            var item = _repo.GetByBarcode(barcode, _warehouse.Id);
            if (item is null) { ShowError($"لم يُعثر على صنف بالباركود: {barcode}"); return; }
            AddItemToCart(item);
            _searchBox.Clear();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void AddSelectedFromList()
    {
        if (_searchList.SelectedIndex < 0 || _searchList.SelectedIndex >= _searchResults.Count) return;
        AddItemToCart(_searchResults[_searchList.SelectedIndex]);
        HideSearchList();
        _searchBox.Clear();
        _searchBox.Focus();
    }

    private void HideSearchList() => _searchList.Height = 0;

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _searchList.Items.Count > 0)
        {
            _searchList.Focus();
            _searchList.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter && _searchList.Items.Count == 1)
        {
            AddSelectedFromList();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            HideSearchList();
            _searchBox.Clear();
        }
    }


    // ══════════════════════════════════════════════════════════
    // إضافة صنف للسلة
    // ══════════════════════════════════════════════════════════
    private void AddItemToCart(Item item)
    {
        // لو الصنف موجود → زود الكمية
        var existing = _lines.FirstOrDefault(l => l.ItemId == item.Id);
        if (existing is not null)
        {
            existing.Quantity++;
            RefreshCartRow(_lines.IndexOf(existing));
            RecalcTotals();
            ShowSuccess($"✔ {item.NameAr} — الكمية: {existing.Quantity}");
            return;
        }

        if (item.CurrentStock <= 0)
        {
            ShowError($"⚠ {item.NameAr} — لا يوجد رصيد في المستودع!");
            // نسمح بالإضافة مع تحذير (يمكن تغييره لمنع الإضافة)
        }

        var line = new SalesInvoiceLine
        {
            ItemId    = item.Id,
            ItemCode  = item.ItemCode,
            ItemName  = item.NameAr,
            UnitName  = item.UnitName,
            Quantity  = 1,
            UnitPrice = item.RetailPrice,
            TaxRate   = item.TaxRate,
            StockQty  = item.CurrentStock
        };
        _lines.Add(line);
        AddCartRow(line);
        RecalcTotals();
        ShowSuccess($"✔ أُضيف: {item.NameAr}");
    }

    private void AddCartRow(SalesInvoiceLine l)
    {
        int idx = _cartGrid.Rows.Add(
            _lines.Count,
            l.ItemName,
            l.UnitName,
            l.Quantity.ToString("N3"),
            l.UnitPrice.ToString("N2"),
            l.Discount.ToString("N2"),
            (l.Quantity * l.UnitPrice - l.Discount).ToString("N2"),
            "🗑"
        );
        if (l.StockQty <= 0)
            _cartGrid.Rows[idx].DefaultCellStyle.ForeColor = Color.Orange;
    }

    private void RefreshCartRow(int rowIdx)
    {
        if (rowIdx < 0 || rowIdx >= _cartGrid.Rows.Count) return;
        var l = _lines[rowIdx];
        _cartGrid.Rows[rowIdx].Cells["colIdx"].Value   = rowIdx + 1;
        _cartGrid.Rows[rowIdx].Cells["colQty"].Value   = l.Quantity.ToString("N3");
        _cartGrid.Rows[rowIdx].Cells["colDisc"].Value  = l.Discount.ToString("N2");
        _cartGrid.Rows[rowIdx].Cells["colTotal"].Value = (l.Quantity * l.UnitPrice - l.Discount).ToString("N2");
    }

    // ══════════════════════════════════════════════════════════
    // تعديل السلة
    // ══════════════════════════════════════════════════════════
    private void CartGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _lines.Count) return;
        var line = _lines[e.RowIndex];
        var col  = _cartGrid.Columns[e.ColumnIndex].Name;

        if (col == "colQty")
        {
            if (!decimal.TryParse(_cartGrid.Rows[e.RowIndex].Cells["colQty"].Value?.ToString(), out decimal qty) || qty <= 0)
            { ShowError("الكمية يجب أن تكون أكبر من صفر."); qty = 1; }
            line.Quantity = qty;
        }
        else if (col == "colDisc")
        {
            decimal.TryParse(_cartGrid.Rows[e.RowIndex].Cells["colDisc"].Value?.ToString(), out decimal disc);
            line.Discount = disc;
        }

        RefreshCartRow(e.RowIndex);
        RecalcTotals();
    }

    private void CartGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_cartGrid.Columns[e.ColumnIndex].Name == "colDel")
        {
            _lines.RemoveAt(e.RowIndex);
            _cartGrid.Rows.RemoveAt(e.RowIndex);
            for (int i = 0; i < _cartGrid.Rows.Count; i++)
                _cartGrid.Rows[i].Cells["colIdx"].Value = i + 1;
            RecalcTotals();
        }
    }

    private void CartGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete) DeleteSelectedLine();
    }

    private void DeleteSelectedLine()
    {
        if (_cartGrid.SelectedRows.Count == 0) return;
        int idx = _cartGrid.SelectedRows[0].Index;
        if (idx < 0 || idx >= _lines.Count) return;
        _lines.RemoveAt(idx);
        _cartGrid.Rows.RemoveAt(idx);
        for (int i = 0; i < _cartGrid.Rows.Count; i++)
            _cartGrid.Rows[i].Cells["colIdx"].Value = i + 1;
        RecalcTotals();
    }

    // ══════════════════════════════════════════════════════════
    // العميل
    // ══════════════════════════════════════════════════════════
    private void CustomerCombo_Changed(object? sender, EventArgs e)
    {
        if (_customerCombo.SelectedItem is ComboItem ci && ci.Id > 0)
        {
            _customerId   = ci.Id;
            _customerName = ci.Name;
        }
        else { _customerId = null; _customerName = string.Empty; }
    }

    // ══════════════════════════════════════════════════════════
    // الإجماليات
    // ══════════════════════════════════════════════════════════
    private void RecalcTotals()
    {
        decimal sub = _lines.Sum(l => l.Quantity * l.UnitPrice - l.Discount);
        decimal.TryParse(_txtInvDiscount.Text, out decimal invDisc);
        _invoiceDiscount = invDisc;
        decimal tax = _lines.Sum(l => l.Quantity * l.UnitPrice * (l.TaxRate / 100m));
        decimal net = sub - invDisc + tax;
        if (net < 0) net = 0;

        _lblSubtotal.Text = sub.ToString("N2");
        _lblDiscount.Text = invDisc.ToString("N2");
        _lblTax.Text      = tax.ToString("N2");
        _lblNet.Text      = net.ToString("N2");

        RecalcChange();
    }

    private void RecalcChange()
    {
        decimal.TryParse(_lblNet.Text.Replace(",", ""), out decimal net);
        decimal.TryParse(_txtPaid.Text, out decimal paid);
        decimal change = paid - net;
        _lblChange.Text      = change >= 0 ? change.ToString("N2") : "—";
        _lblChange.ForeColor = change >= 0 ? Color.Gold : Color.OrangeRed;
    }

    // ══════════════════════════════════════════════════════════
    // طريقة الدفع
    // ══════════════════════════════════════════════════════════
    private void SetPayMethod(string method)
    {
        _payMethod = method;
        Color border = Color.Gold;
        _btnCash.FlatAppearance.BorderSize   = method == "cash"   ? 2 : 0;
        _btnVisa.FlatAppearance.BorderSize   = method == "visa"   ? 2 : 0;
        _btnCredit.FlatAppearance.BorderSize = method == "credit" ? 2 : 0;
        _btnCash.FlatAppearance.BorderColor   = border;
        _btnVisa.FlatAppearance.BorderColor   = border;
        _btnCredit.FlatAppearance.BorderColor = border;

        // الدفع الآجل: مدفوع = صفر
        if (method == "credit") _txtPaid.Text = "0";
        _txtPaid.Enabled = method != "credit";
    }


    // ══════════════════════════════════════════════════════════
    // معالجة الدفع
    // ══════════════════════════════════════════════════════════
    private void ProcessPayment()
    {
        if (_lines.Count == 0) { ShowError("السلة فارغة — أضف أصناف أولاً."); return; }

        decimal.TryParse(_lblNet.Text.Replace(",", ""), out decimal net);
        decimal.TryParse(_txtPaid.Text, out decimal paid);

        if (_payMethod == "cash" && paid < net)
        { ShowError($"المبلغ المدفوع ({paid:N2}) أقل من الصافي ({net:N2})."); _txtPaid.Focus(); return; }

        if (_payMethod == "credit" && _customerId is null)
        { ShowError("الدفع الآجل يتطلب اختيار عميل."); _customerCombo.Focus(); return; }

        try
        {
            decimal.TryParse(_lblSubtotal.Text.Replace(",", ""), out decimal sub);
            decimal.TryParse(_lblTax.Text.Replace(",", ""),      out decimal tax);

            var inv = new SalesInvoice
            {
                InvoiceNumber  = _lblInvNum.Text.Replace("🧾 فاتورة:  ", "").Trim(),
                CustomerId     = _customerId,
                CustomerName   = _customerName,
                WarehouseId    = _warehouse.Id,
                CashierId      = SessionContext.CurrentUser?.Id ?? 1,
                InvoiceDate    = DateTime.Now,
                PaymentMethod  = _payMethod,
                Subtotal       = sub,
                Discount       = _invoiceDiscount,
                TaxAmount      = tax,
                NetTotal       = net,
                PaidAmount     = _payMethod == "credit" ? 0 : paid,
                ChangeAmount   = _payMethod == "cash" ? paid - net : 0,
                Status         = "completed",
                Lines          = new List<SalesInvoiceLine>(_lines)
            };

            int invoiceId = _repo.SaveInvoice(inv);

            // طباعة الإيصال
            PrintReceipt(inv, invoiceId);

            ShowSuccess($"✅ تم تسجيل الفاتورة بنجاح — الباقي: {inv.ChangeAmount:N2} ج.م");
            System.Threading.Thread.Sleep(800);
            NewInvoice();
        }
        catch (Exception ex) { ShowError($"خطأ في الحفظ: {ex.Message}"); }
    }

    // ══════════════════════════════════════════════════════════
    // طباعة الإيصال (Thermal / A4)
    // ══════════════════════════════════════════════════════════
    private void PrintReceipt(SalesInvoice inv, int invoiceId)
    {
        try
        {
            var pd = new System.Drawing.Printing.PrintDocument();
            pd.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Receipt", 302, 1000);

            pd.PrintPage += (_, e) =>
            {
                if (e.Graphics is null) return;
                var g    = e.Graphics;
                float y  = 10;
                float w  = e.PageBounds.Width - 20;

                var boldFont  = new Font("Tahoma", 11F, FontStyle.Bold);
                var normFont  = new Font("Tahoma", 9F);
                var smallFont = new Font("Tahoma", 8F);
                var bigFont   = new Font("Tahoma", 14F, FontStyle.Bold);
                var brush     = Brushes.Black;
                var fmt       = new StringFormat { Alignment = StringAlignment.Center };
                var fmtR      = new StringFormat { Alignment = StringAlignment.Far };

                g.DrawString("Smart Market", bigFont, brush, new RectangleF(0, y, w, 28), fmt); y += 30;
                g.DrawString($"فاتورة رقم: {inv.InvoiceNumber}", boldFont, brush, new RectangleF(0, y, w, 20), fmt); y += 22;
                g.DrawString($"التاريخ: {inv.InvoiceDate:dd/MM/yyyy hh:mm tt}", smallFont, brush, new RectangleF(0, y, w, 16), fmt); y += 18;
                if (!string.IsNullOrEmpty(inv.CustomerName))
                { g.DrawString($"العميل: {inv.CustomerName}", normFont, brush, new RectangleF(0, y, w, 16), fmt); y += 18; }

                // خط فاصل
                g.DrawLine(Pens.Black, 10, y, w, y); y += 6;

                foreach (var l in inv.Lines)
                {
                    g.DrawString(l.ItemName, normFont, brush, 10, y); y += 16;
                    string lineInfo = $"{l.Quantity:N0} × {l.UnitPrice:N2} = {(l.Quantity * l.UnitPrice - l.Discount):N2}";
                    g.DrawString(lineInfo, smallFont, brush, new RectangleF(0, y, w, 14), fmtR); y += 16;
                }

                g.DrawLine(Pens.Black, 10, y, w, y); y += 6;

                g.DrawString($"الصافي:    {inv.NetTotal:N2} ج.م", boldFont, brush, new RectangleF(0, y, w, 20), fmtR); y += 22;
                if (inv.PaymentMethod == "cash")
                {
                    g.DrawString($"المدفوع:  {inv.PaidAmount:N2} ج.م", normFont, brush, new RectangleF(0, y, w, 18), fmtR); y += 18;
                    g.DrawString($"الباقي:    {inv.ChangeAmount:N2} ج.م", normFont, brush, new RectangleF(0, y, w, 18), fmtR); y += 18;
                }
                g.DrawLine(Pens.Black, 10, y, w, y); y += 8;
                g.DrawString("شكراً لتسوقكم معنا", normFont, brush, new RectangleF(0, y, w, 20), fmt);

                e.HasMorePages = false;
            };

            // عرض نافذة الطباعة
            using var pd2 = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
            pd2.ShowDialog();
        }
        catch { /* تجاهل أخطاء الطباعة */ }
    }

    // ══════════════════════════════════════════════════════════
    // تعليق واستئناف الفاتورة
    // ══════════════════════════════════════════════════════════
    private void HoldInvoice()
    {
        if (_lines.Count == 0) { ShowError("السلة فارغة — لا شيء للتعليق."); return; }

        string custRef = "";
        using (var dlg = new Form
        {
            Text = "تعليق الفاتورة", Width = 360, Height = 150,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
            RightToLeft = RightToLeft.Yes, RightToLeftLayout = true
        })
        {
            var lbl = new Label { Text = "مرجع / اسم العميل (اختياري):", Location = new Point(10, 20), AutoSize = true };
            var txt = new TextBox { Location = new Point(10, 45), Width = 320 };
            var btn = new Button { Text = "تعليق", Location = new Point(120, 78), Width = 100, DialogResult = DialogResult.OK };
            dlg.Controls.AddRange(new Control[] { lbl, txt, btn });
            dlg.AcceptButton = btn;
            if (dlg.ShowDialog() == DialogResult.OK) custRef = txt.Text.Trim();
        }

        decimal.TryParse(_lblSubtotal.Text.Replace(",", ""), out decimal sub);
        _held.Add(new HoldInvoice
        {
            HoldRef      = $"H-{DateTime.Now:HHmmss}",
            CustomerRef  = custRef,
            Lines        = new List<SalesInvoiceLine>(_lines),
            Subtotal     = sub,
            CustomerId   = _customerId,
            CustomerName = _customerName,
            HeldAt       = DateTime.Now
        });

        ShowSuccess($"✔ تم تعليق الفاتورة [{custRef}] — لديك {_held.Count} معلقة.");
        NewInvoice();
    }

    private void ResumeInvoice()
    {
        if (_held.Count == 0) { ShowInfo("لا توجد فواتير معلقة."); return; }

        using var dlg = new Form
        {
            Text = "الفواتير المعلقة", Width = 500, Height = 320,
            StartPosition = FormStartPosition.CenterParent,
            RightToLeft = RightToLeft.Yes, RightToLeftLayout = true
        };
        var lst = new ListBox { Dock = DockStyle.Fill, Font = new Font("Tahoma", 10F) };
        foreach (var h in _held)
            lst.Items.Add($"{h.HoldRef}  |  {h.CustomerRef}  |  {h.Subtotal:N2} ج.م  |  {h.HeldAt:hh:mm tt}");

        var btnOk = new Button { Text = "استئناف", Dock = DockStyle.Bottom, Height = 36, DialogResult = DialogResult.OK };
        dlg.Controls.Add(lst); dlg.Controls.Add(btnOk); dlg.AcceptButton = btnOk;

        if (dlg.ShowDialog() != DialogResult.OK || lst.SelectedIndex < 0) return;

        var chosen = _held[lst.SelectedIndex];
        _held.RemoveAt(lst.SelectedIndex);

        // استرجاع بيانات الفاتورة
        NewInvoice();
        _customerId   = chosen.CustomerId;
        _customerName = chosen.CustomerName;
        foreach (var l in chosen.Lines)
        {
            _lines.Add(l);
            AddCartRow(l);
        }
        if (chosen.CustomerId.HasValue)
        {
            for (int i = 0; i < _customerCombo.Items.Count; i++)
                if (_customerCombo.Items[i] is ComboItem ci && ci.Id == chosen.CustomerId)
                { _customerCombo.SelectedIndex = i; break; }
        }
        RecalcTotals();
        ShowSuccess($"✔ تم استئناف الفاتورة: {chosen.HoldRef}");
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════
    private void ShowSuccess(string m) { _lblFeedback.ForeColor = AppTheme.Success; _lblFeedback.Text = m; }
    private void ShowError(string m)   { _lblFeedback.ForeColor = AppTheme.Danger;  _lblFeedback.Text = m; }
    private void ShowInfo(string m)    { _lblFeedback.ForeColor = Color.Silver;     _lblFeedback.Text = m; }
}

// ── ComboItem helper ─────────────────────────────────────────
file sealed class ComboItem
{
    public int    Id   { get; }
    public string Name { get; }
    public ComboItem(int id, string name) { Id = id; Name = name; }
    public override string ToString() => Name;
}
