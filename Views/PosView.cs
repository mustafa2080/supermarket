using System.Drawing;
using System.Drawing.Drawing2D;
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
    private static readonly Color PosBg           = ColorTranslator.FromHtml("#EEF3F8");
    private static readonly Color Surface         = Color.White;
    private static readonly Color SurfaceAlt      = ColorTranslator.FromHtml("#F8FBFF");
    private static readonly Color Ink             = ColorTranslator.FromHtml("#14324A");
    private static readonly Color MutedInk        = ColorTranslator.FromHtml("#6A8094");
    private static readonly Color Border          = ColorTranslator.FromHtml("#D7E4F0");
    private static readonly Color HeaderBg        = ColorTranslator.FromHtml("#163A59");
    private static readonly Color HeaderAccent    = ColorTranslator.FromHtml("#E38B17");
    private static readonly Color SuccessGreen    = ColorTranslator.FromHtml("#1F8A5B");
    private static readonly Color VisaBlue        = ColorTranslator.FromHtml("#2B6CB0");
    private static readonly Color CreditBrown     = ColorTranslator.FromHtml("#A15C22");
    private static readonly Color CardShadow      = Color.FromArgb(26, 22, 57, 93);

    // ── بيانات الجلسة ────────────────────────────────────────
    private readonly SalesRepository _repo   = new();
    private Warehouse  _warehouse            = new() { Id = 1, Name = "الرئيسي" };
    private List<Customer>  _customers       = new();
    private List<Promotion> _activePromotions = new();   // TASK-016
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
    private SplitContainer?         _mainSplit;

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
        BackColor   = PosBg;
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
            _warehouse        = _repo.GetDefaultWarehouse() ?? _warehouse;
            _customers        = _repo.GetCustomers();
            _activePromotions = new PromotionRepository().GetActive(); // TASK-016
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

        // اللوح الأيمن فعلياً: بحث + سلة
        var salesPanel = BuildLeftPanel();

        // اللوح الأيسر فعلياً: إجماليات + دفع
        var summaryPanel = BuildRightPanel();

        var split = new SplitContainer
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Vertical,
            SplitterWidth = 8,
            BackColor     = PosBg,
            RightToLeft   = RightToLeft.No
        };
        _mainSplit = split;
        split.Panel1.Controls.Add(summaryPanel);
        split.Panel2.Controls.Add(salesPanel);

        split.SizeChanged += (_, _) => UpdateMainSplitLayout();
        split.Layout      += (_, _) => UpdateMainSplitLayout();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = PosBg,
            Padding = new Padding(16, 14, 16, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(split,  0, 1);
        Controls.Add(root);

        // نستخدم HandleCreated على this بدل BeginInvoke المباشر
        // لأن الـ Handle لسه مش اتعمل في وقت الـ constructor
        HandleCreated += (_, _) => BeginInvoke(new Action(UpdateMainSplitLayout));
    }

    private void UpdateMainSplitLayout()
    {
        if (_mainSplit is null || _mainSplit.Width <= 0) return;

        try
        {
            const int p1Min = 360;
            const int p2Min = 560;

            // نضبط MinSizes هنا بأمان لأن الـ Width بقى له قيمة حقيقية
            if (_mainSplit.Panel1MinSize != p1Min) _mainSplit.Panel1MinSize = p1Min;
            if (_mainSplit.Panel2MinSize != p2Min) _mainSplit.Panel2MinSize = p2Min;

            int min = p1Min;
            int max = _mainSplit.Width - p2Min - _mainSplit.SplitterWidth;
            if (max <= 0) return;

            if (max < min)
            {
                _mainSplit.IsSplitterFixed = true;
                return;
            }

            int desired     = (int)((_mainSplit.Width - _mainSplit.SplitterWidth) * 0.35);
            int safeDistance = Math.Max(min, Math.Min(desired, max));

            if (_mainSplit.SplitterDistance != safeDistance)
                _mainSplit.SplitterDistance = safeDistance;
        }
        catch
        {
            // نتجاهل أي حالة عابرة أثناء أول layout
        }
    }

    // ── شريط العنوان ─────────────────────────────────────────
    private Control BuildTopBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = HeaderBg,
            Padding   = new Padding(18, 12, 18, 10)
        };
        bar.Paint += (_, e) =>
        {
            e.Graphics.FillRectangle(new SolidBrush(HeaderAccent), 0, 0, bar.Width, 4);
        };

        var titleLbl = new Label
        {
            Text = "نقطة البيع",
            Dock = DockStyle.Right,
            Width = 180,
            Font = new Font("Tahoma", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight
        };

        _lblInvNum.AutoSize  = true;
        _lblInvNum.Font      = new Font("Tahoma", 11F, FontStyle.Bold);
        _lblInvNum.ForeColor = Color.White;
        _lblInvNum.Location  = new Point(18, 22);

        _lblCashier.AutoSize  = true;
        _lblCashier.Font      = new Font("Tahoma", 9.5F);
        _lblCashier.ForeColor = Color.FromArgb(210, 225, 238);
        _lblCashier.Location  = new Point(250, 24);
        _lblCashier.Text      = $"الكاشير: {SessionContext.DisplayName}  |  المستودع: {_warehouse.Name}";

        _lblTime.AutoSize  = true;
        _lblTime.Font      = new Font("Tahoma", 14F, FontStyle.Bold);
        _lblTime.ForeColor = Color.FromArgb(255, 233, 174);
        _lblTime.Text      = DateTime.Now.ToString("hh:mm:ss tt");
        _lblTime.Anchor    = AnchorStyles.Left | AnchorStyles.Top;
        _lblTime.Location  = new Point(bar.Width - 170, 20);

        _lblFeedback.AutoSize  = false;
        _lblFeedback.Dock      = DockStyle.Bottom;
        _lblFeedback.Height    = 0; // مخفي افتراضياً
        _lblFeedback.Font      = AppTheme.BodyFont;
        _lblFeedback.TextAlign = ContentAlignment.MiddleRight;
        _lblFeedback.ForeColor = AppTheme.Success;

        bar.Controls.Add(titleLbl);
        bar.Controls.Add(_lblInvNum);
        bar.Controls.Add(_lblCashier);
        bar.Controls.Add(_lblTime);
        return bar;
    }

    // ── اللوح الأيمن: بحث + عميل + سلة ─────────────────────
    private Control BuildLeftPanel()
    {
        var p = CreateCardPanel(new Padding(18, 16, 18, 16));

        // صف البحث
        var sectionHeader = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        var sectionTitle = new Label
        {
            Text = "سلة المبيعات",
            Dock = DockStyle.Right,
            Width = 180,
            Font = new Font("Tahoma", 14F, FontStyle.Bold),
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleRight
        };
        var sectionSub = new Label
        {
            Text = "ابحث عن الصنف ثم أضفه مباشرة إلى الفاتورة",
            Dock = DockStyle.Fill,
            Font = new Font("Tahoma", 9F),
            ForeColor = MutedInk,
            TextAlign = ContentAlignment.MiddleRight
        };
        sectionHeader.Controls.Add(sectionSub);
        sectionHeader.Controls.Add(sectionTitle);

        var searchRow = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 6) };
        var lblSearch = new Label
        {
            Text = "بحث سريع",
            Dock = DockStyle.Right,
            Width = 88,
            ForeColor = Ink,
            Font = new Font("Tahoma", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        _searchBox.Dock        = DockStyle.Fill;
        _searchBox.Font        = new Font("Tahoma", 11.5F);
        _searchBox.BackColor   = SurfaceAlt;
        _searchBox.ForeColor   = Ink;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.PlaceholderText = "اسم الصنف أو الباركود أو الكود...";
        _searchBox.TextAlign   = HorizontalAlignment.Right;
        searchRow.Controls.Add(_searchBox);
        searchRow.Controls.Add(lblSearch);

        // قائمة نتائج البحث (Dropdown)
        _searchList.Dock             = DockStyle.Top;
        _searchList.Height           = 0; // مخفية في البداية
        _searchList.BackColor        = Surface;
        _searchList.ForeColor        = Ink;
        _searchList.Font             = new Font("Tahoma", 10F);
        _searchList.BorderStyle      = BorderStyle.FixedSingle;
        _searchList.ItemHeight       = 28;
        _searchList.IntegralHeight   = false;

        // صف العميل
        var customerRow = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 8) };
        var lblCust = new Label
        {
            Text = "العميل",
            Dock = DockStyle.Right,
            Width = 72,
            ForeColor = Ink,
            Font = new Font("Tahoma", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        _customerCombo.Dock         = DockStyle.Fill;
        _customerCombo.BackColor    = SurfaceAlt;
        _customerCombo.ForeColor    = Ink;
        _customerCombo.FlatStyle    = FlatStyle.Flat;
        customerRow.Controls.Add(_customerCombo);
        customerRow.Controls.Add(lblCust);

        // السلة
        BuildCartGrid();
        _cartGrid.Dock = DockStyle.Fill;

        // شريط feedback
        _lblFeedback.Dock      = DockStyle.Bottom;
        _lblFeedback.Height    = 32;
        _lblFeedback.Font      = new Font("Tahoma", 9F);
        _lblFeedback.TextAlign = ContentAlignment.MiddleRight;
        _lblFeedback.BackColor = SurfaceAlt;
        _lblFeedback.ForeColor = SuccessGreen;

        p.Controls.Add(_cartGrid);
        p.Controls.Add(_searchList);
        p.Controls.Add(customerRow);
        p.Controls.Add(searchRow);
        p.Controls.Add(sectionHeader);
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
        _cartGrid.BackgroundColor       = Surface;
        _cartGrid.GridColor             = Border;
        _cartGrid.DefaultCellStyle.BackColor = Surface;
        _cartGrid.DefaultCellStyle.ForeColor = Ink;
        _cartGrid.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DDEEFF");
        _cartGrid.DefaultCellStyle.SelectionForeColor = Ink;
        _cartGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _cartGrid.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F8FBFE");
        _cartGrid.ColumnHeadersDefaultCellStyle.BackColor   = ColorTranslator.FromHtml("#EDF4FB");
        _cartGrid.ColumnHeadersDefaultCellStyle.ForeColor   = Ink;
        _cartGrid.ColumnHeadersDefaultCellStyle.Font        = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _cartGrid.ColumnHeadersDefaultCellStyle.Alignment   = DataGridViewContentAlignment.MiddleCenter;
        _cartGrid.EnableHeadersVisualStyles = false;
        _cartGrid.RowTemplate.Height        = 40;

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
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.BackColor  = ColorTranslator.FromHtml("#EAF8F0");
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.ForeColor  = SuccessGreen;
        _cartGrid.Columns["colQty"]!.DefaultCellStyle.Font       = new Font("Tahoma", 11F, FontStyle.Bold);
        _cartGrid.Columns["colDisc"]!.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#FFF4E8");
        _cartGrid.Columns["colDisc"]!.DefaultCellStyle.ForeColor = HeaderAccent;
    }

    // ══════════════════════════════════════════════════════════
    // اللوح الأيمن: الإجماليات + الدفع + الأزرار
    // ══════════════════════════════════════════════════════════
    private Control BuildRightPanel()
    {
        var p = CreateCardPanel(new Padding(16, 16, 16, 16));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 144F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(new Label
        {
            Text = "لوحة الدفع والملخص",
            Dock = DockStyle.Fill,
            Font = new Font("Tahoma", 14F, FontStyle.Bold),
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleRight
        }, 0, 0);
        layout.Controls.Add(BuildTotalsPanel(),    0, 1);
        layout.Controls.Add(BuildPayMethodPanel(), 0, 2);
        layout.Controls.Add(BuildPaidPanel(),      0, 3);
        layout.Controls.Add(BuildActionPanel(),    0, 4);
        p.Controls.Add(layout);
        return p;
    }

    private Control BuildTotalsPanel()
    {
        var card = CreateInnerCard(new Padding(14));
        _lblSubtotal.Text = "0.00"; _lblSubtotal.AutoSize = true; _lblSubtotal.ForeColor = Ink;            _lblSubtotal.Font = new Font("Tahoma", 10.5F, FontStyle.Bold);
        _lblDiscount.Text = "0.00"; _lblDiscount.AutoSize = true; _lblDiscount.ForeColor = HeaderAccent;   _lblDiscount.Font = new Font("Tahoma", 10.5F, FontStyle.Bold);
        _lblTax.Text      = "0.00"; _lblTax.AutoSize      = true; _lblTax.ForeColor      = VisaBlue;       _lblTax.Font      = new Font("Tahoma", 10.5F, FontStyle.Bold);
        _lblNet.Text      = "0.00"; _lblNet.AutoSize      = true; _lblNet.ForeColor      = SuccessGreen;   _lblNet.Font      = new Font("Tahoma", 17F, FontStyle.Bold);
        var tbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, BackColor = Color.Transparent };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        for (int i = 0; i < 4; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        Label Lbl(string t, bool big = false) => new Label
        {
            Text = t,
            AutoSize = true,
            ForeColor = big ? Ink : MutedInk,
            Font = big ? new Font("Tahoma", 12F, FontStyle.Bold) : new Font("Tahoma", 9.5F, FontStyle.Bold),
            Anchor = AnchorStyles.Right
        };
        tbl.Controls.Add(Lbl("المجموع الفرعي:"),       0, 0); tbl.Controls.Add(_lblSubtotal, 1, 0);
        tbl.Controls.Add(Lbl("الخصم:"),                0, 1); tbl.Controls.Add(_lblDiscount, 1, 1);
        tbl.Controls.Add(Lbl("الضريبة:"),              0, 2); tbl.Controls.Add(_lblTax,      1, 2);
        tbl.Controls.Add(Lbl("الصافي:", big: true),    0, 3); tbl.Controls.Add(_lblNet,      1, 3);
        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildPayMethodPanel()
    {
        var p    = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        void StylePayBtn(Button b, Color col)
        {
            b.Width = 96; b.Height = 42; b.Font = new Font("Tahoma", 9.5F, FontStyle.Bold);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = col; b.ForeColor = Color.White;
            b.Margin = new Padding(4, 0, 8, 0);
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.BorderColor = Color.Gold;
        }
        StylePayBtn(_btnCash,   SuccessGreen);
        StylePayBtn(_btnVisa,   VisaBlue);
        StylePayBtn(_btnCredit, CreditBrown);
        flow.Controls.Add(_btnCash); flow.Controls.Add(_btnVisa); flow.Controls.Add(_btnCredit);
        p.Controls.Add(flow);
        return p;
    }

    private Control BuildPaidPanel()
    {
        var card = CreateInnerCard(new Padding(12, 10, 12, 10));
        var tbl  = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            RightToLeft = RightToLeft.Yes
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        for (int i = 0; i < 3; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
        _txtInvDiscount.Width = 110; _txtInvDiscount.BackColor = SurfaceAlt; _txtInvDiscount.ForeColor = HeaderAccent;
        _txtInvDiscount.Font = new Font("Tahoma", 10.5F, FontStyle.Bold); _txtInvDiscount.BorderStyle = BorderStyle.FixedSingle; _txtInvDiscount.Text = "0";
        _txtInvDiscount.TextAlign = HorizontalAlignment.Right;
        _txtPaid.Width = 130; _txtPaid.BackColor = ColorTranslator.FromHtml("#EAF8F0"); _txtPaid.ForeColor = SuccessGreen;
        _txtPaid.Font  = new Font("Tahoma", 12F, FontStyle.Bold); _txtPaid.BorderStyle = BorderStyle.FixedSingle;
        _txtPaid.TextAlign = HorizontalAlignment.Right;
        _lblChange.AutoSize = true; _lblChange.Text = "0.00"; _lblChange.ForeColor = Ink;
        _lblChange.Font = new Font("Tahoma", 14F, FontStyle.Bold);
        Label Lbl(string t, Color c) => new Label { Text = t, AutoSize = true, ForeColor = c, Font = new Font("Tahoma", 9.5F, FontStyle.Bold), Margin = new Padding(0, 8, 4, 0) };
        tbl.Controls.Add(Lbl("خصم الفاتورة:", Color.Orange),     0, 0); tbl.Controls.Add(_txtInvDiscount, 1, 0);
        tbl.Controls.Add(Lbl("المدفوع:",       Color.LightGreen), 0, 1); tbl.Controls.Add(_txtPaid,        1, 1);
        tbl.Controls.Add(Lbl("الباقي:",        Color.Gold),       0, 2); tbl.Controls.Add(_lblChange,      1, 2);
        card.Controls.Add(tbl);
        return card;
    }

    private Control BuildActionPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
        var note = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Text = "راجع الصافي وطريقة الدفع قبل إنهاء الفاتورة",
            Font = new Font("Tahoma", 8.8F),
            ForeColor = MutedInk,
            TextAlign = ContentAlignment.MiddleCenter
        };
        void StyleBtn(Button b, Color col, int h = 48)
        {
            b.Dock = DockStyle.Top; b.Height = h; b.Font = new Font("Tahoma", 10.5F, FontStyle.Bold);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = col; b.ForeColor = Color.White;
            b.Margin = new Padding(0, 0, 0, 8); b.FlatAppearance.BorderSize = 0;
        }
        StyleBtn(_btnPay,    SuccessGreen, 50);
        StyleBtn(_btnHold,   ColorTranslator.FromHtml("#8A6B2F"), 46);
        StyleBtn(_btnResume, VisaBlue, 40);
        // Dock.Top: المضاف أولاً يظهر في الأسفل، لذا نعكس الترتيب
        p.Controls.Add(_btnResume);
        p.Controls.Add(_btnHold);
        p.Controls.Add(_btnPay);
        p.Controls.Add(note);
        return p;
    }

    private static Panel CreateCardPanel(Padding padding)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = padding
        };

        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var shadowRect = new Rectangle(4, 6, panel.Width - 10, panel.Height - 10);
            using var shadowPath = CreateRoundedPath(shadowRect, 22);
            using var shadowBrush = new SolidBrush(CardShadow);
            e.Graphics.FillPath(shadowBrush, shadowPath);

            var rect = new Rectangle(0, 0, panel.Width - 12, panel.Height - 12);
            using var path = CreateRoundedPath(rect, 22);
            using var fill = new SolidBrush(Surface);
            using var border = new Pen(Border);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        };

        return panel;
    }

    private static Panel CreateInnerCard(Padding padding)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceAlt,
            Padding = padding
        };

        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = CreateRoundedPath(rect, 16);
            using var fill = new SolidBrush(SurfaceAlt);
            using var border = new Pen(Border);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        };

        return panel;
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
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
        // لو الصنف موجود → زود الكمية وأعد حساب العرض
        var existing = _lines.FirstOrDefault(l => l.ItemId == item.Id);
        if (existing is not null)
        {
            existing.Quantity++;
            ApplyPromotion(existing, item);
            RefreshCartRow(_lines.IndexOf(existing));
            RecalcTotals();
            ShowSuccess($"✔ {item.NameAr} — الكمية: {existing.Quantity}");
            return;
        }

        if (item.CurrentStock <= 0)
            ShowError($"⚠ {item.NameAr} — لا يوجد رصيد في المستودع!");

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

        // تطبيق أفضل عرض متاح — TASK-016
        ApplyPromotion(line, item);

        _lines.Add(line);
        AddCartRow(line);
        RecalcTotals();

        string msg = $"✔ أُضيف: {item.NameAr}";
        if (!string.IsNullOrEmpty(line.PromotionName))
            msg += $"  🎁 {line.PromotionName}";
        ShowSuccess(msg);
    }

    /// <summary>تطبيق أفضل عرض نشط على سطر — يُحدِّث Discount وPromotionId</summary>
    private void ApplyPromotion(SalesInvoiceLine line, Item item)
    {
        var result = PromotionEngine.Apply(item, line.Quantity, _activePromotions);
        if (result is not null)
        {
            line.Discount      = result.DiscountAmount;
            line.PromotionId   = result.PromotionId;
            line.PromotionName = result.Description;
        }
        else
        {
            // إزالة أي عرض سابق لو لم ينطبق بعد تغيير الكمية
            if (line.PromotionId.HasValue)
            {
                line.Discount      = 0;
                line.PromotionId   = null;
                line.PromotionName = string.Empty;
            }
        }
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
