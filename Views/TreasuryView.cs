using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة الخزينة — سندات الصرف والقبض — TASK-022</summary>
internal class TreasuryView : UserControl
{
    private readonly TreasuryRepository _repo = new();

    // تابات
    private TabControl _tabs        = null!;
    private TabPage    _tabPay      = null!;
    private TabPage    _tabRec      = null!;
    private TabPage    _tabSafes    = null!;
    private TabPage    _tabShifts   = null!;

    // جدول الصرف
    private DataGridView _gridPay    = null!;
    private Label        _lblPayStat = null!;

    // جدول القبض
    private DataGridView _gridRec    = null!;
    private Label        _lblRecStat = null!;

    // جدول الخزائن
    private DataGridView _gridSafes  = null!;
    private Label        _lblSafesStat = null!;

    private List<PaymentVoucher> _payments  = new();
    private List<ReceiptVoucher> _receipts  = new();
    private List<Safe>           _safes     = new();

    public TreasuryView()
    {
        InitializeComponent();
        LoadAll();
    }

    private void InitializeComponent()
    {
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };

        _tabPay    = new TabPage("💸 سندات الصرف")  { BackColor = AppTheme.Surface };
        _tabRec    = new TabPage("💰 سندات القبض")  { BackColor = AppTheme.Surface };
        _tabSafes  = new TabPage("🏦 الخزائن")       { BackColor = AppTheme.Surface };
        _tabShifts = new TabPage("🕐 الورديات")      { BackColor = AppTheme.Surface };

        _tabPay.Controls.Add(BuildPayPanel());
        _tabRec.Controls.Add(BuildRecPanel());
        _tabSafes.Controls.Add(BuildSafesPanel());
        _tabShifts.Controls.Add(new ShiftView { Dock = DockStyle.Fill });

        _tabs.TabPages.Add(_tabShifts);   // الورديات أول تاب (الأهم)
        _tabs.TabPages.Add(_tabPay);
        _tabs.TabPages.Add(_tabRec);
        _tabs.TabPages.Add(_tabSafes);

        Controls.Add(_tabs);
    }

    // ── تاب سندات الصرف ──────────────────────────────────────
    private Control BuildPayPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        var toolbar = BuildToolbar(
            "➕ سند صرف جديد", AppTheme.Danger,
            () => {
                using var dlg = new VoucherDialog(VoucherDialog.VoucherType.Payment);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
            });

        _gridPay = BuildGrid(new[]
        {
            ("Num",     "رقم السند",    130),
            ("Date",    "التاريخ",       100),
            ("Safe",    "الخزينة",       130),
            ("Expense", "بند المصروف",   180),
            ("Desc",    "الوصف",         200),
            ("Amount",  "المبلغ",         110),
            ("Type",    "النوع",           90),
            ("By",      "أنشأه",          120),
        });

        _lblPayStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0)
        };

        pnl.Controls.Add(_gridPay);
        pnl.Controls.Add(toolbar);
        pnl.Controls.Add(_lblPayStat);
        return pnl;
    }

    // ── تاب سندات القبض ──────────────────────────────────────
    private Control BuildRecPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        var toolbar = BuildToolbar(
            "➕ سند قبض جديد", AppTheme.Success,
            () => {
                using var dlg = new VoucherDialog(VoucherDialog.VoucherType.Receipt);
                if (dlg.ShowDialog(this) == DialogResult.OK) LoadAll();
            });

        _gridRec = BuildGrid(new[]
        {
            ("Num",    "رقم السند",     130),
            ("Date",   "التاريخ",        100),
            ("Safe",   "الخزينة",        130),
            ("Source", "مصدر الإيراد",   180),
            ("Desc",   "الوصف",          200),
            ("Amount", "المبلغ",          110),
            ("Type",   "النوع",            90),
            ("By",     "أنشأه",           120),
        });

        _lblRecStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0)
        };

        pnl.Controls.Add(_gridRec);
        pnl.Controls.Add(toolbar);
        pnl.Controls.Add(_lblRecStat);
        return pnl;
    }

    // ── تاب الخزائن ──────────────────────────────────────────
    private Control BuildSafesPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        var toolbar = BuildToolbar("🔄 تحديث", AppTheme.Primary, LoadAll);

        _gridSafes = BuildGrid(new[]
        {
            ("Name",    "اسم الخزينة",  200),
            ("Balance", "الرصيد الحالي",160),
            ("Default", "افتراضية",      100),
        });

        _lblSafesStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0)
        };

        pnl.Controls.Add(_gridSafes);
        pnl.Controls.Add(toolbar);
        pnl.Controls.Add(_lblSafesStat);
        return pnl;
    }

    // ── مساعدات بناء الـ UI ───────────────────────────────────
    private static Panel BuildToolbar(string btnText, Color btnColor, Action onClick)
    {
        var tb = new Panel
        {
            Dock = DockStyle.Top, Height = 52,
            BackColor = AppTheme.Surface, Padding = new Padding(10, 10, 10, 10)
        };
        var btn = new Button
        {
            Text = btnText, Width = 170, Dock = DockStyle.Right,
            BackColor = btnColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => onClick();

        var btnRefresh = new Button
        {
            Text = "🔄 تحديث", Width = 100, Dock = DockStyle.Right,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat
        };
        btnRefresh.FlatAppearance.BorderColor = AppTheme.Border;
        btnRefresh.Click += (_, _) => { /* LoadAll يتم من الـ parent */ };

        tb.Controls.Add(btn);
        return tb;
    }

    private static DataGridView BuildGrid((string name, string header, int width)[] cols)
    {
        var g = new DataGridView
        {
            Dock                  = DockStyle.Fill,
            BackgroundColor       = AppTheme.Surface,
            GridColor             = AppTheme.Border,
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate           = { Height = 33 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 38,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Primary, ForeColor = Color.White,
                Font = AppTheme.SectionFont, Padding = new Padding(6)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
                Padding = new Padding(4),
                SelectionBackColor = Color.FromArgb(187, 222, 251),
                SelectionForeColor = AppTheme.DarkText
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Background }
        };
        foreach (var (name, header, width) in cols)
            g.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width });
        return g;
    }

    // ══ تحميل البيانات ═══════════════════════════════════════

    private void LoadAll()
    {
        try
        {
            _safes    = _repo.GetSafes();
            _payments = _repo.GetPaymentVouchers();
            _receipts = _repo.GetReceiptVouchers();
            BindPayments();
            BindReceipts();
            BindSafes();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل بيانات الخزينة:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindPayments()
    {
        _gridPay.Rows.Clear();
        decimal total = 0;
        foreach (var p in _payments)
        {
            _gridPay.Rows.Add(
                p.VoucherNumber, p.VoucherDate.ToString("yyyy-MM-dd"),
                p.SafeName, p.ExpenseItemName, p.Description,
                p.Amount.ToString("N2") + " ج.م", p.TypeAr, p.CreatedBy);
            total += p.Amount;
        }
        _lblPayStat.Text =
            $"إجمالي السندات: {_payments.Count}  |  إجمالي الصرف: {total:N2} ج.م";
    }

    private void BindReceipts()
    {
        _gridRec.Rows.Clear();
        decimal total = 0;
        foreach (var r in _receipts)
        {
            _gridRec.Rows.Add(
                r.VoucherNumber, r.VoucherDate.ToString("yyyy-MM-dd"),
                r.SafeName, r.RevenueSourceName, r.Description,
                r.Amount.ToString("N2") + " ج.م", r.TypeAr, r.CreatedBy);
            total += r.Amount;
        }
        _lblRecStat.Text =
            $"إجمالي السندات: {_receipts.Count}  |  إجمالي القبض: {total:N2} ج.م";
    }

    private void BindSafes()
    {
        _gridSafes.Rows.Clear();
        decimal totalBal = 0;
        foreach (var s in _safes)
        {
            int row = _gridSafes.Rows.Add(
                s.NameAr,
                s.Balance.ToString("N2") + " ج.م",
                s.IsDefault ? "✔ نعم" : "");
            if (s.Balance < 0)
                _gridSafes.Rows[row].DefaultCellStyle.ForeColor = AppTheme.Danger;
            totalBal += s.Balance;
        }
        _lblSafesStat.Text =
            $"عدد الخزائن: {_safes.Count}  |  إجمالي الأرصدة: {totalBal:N2} ج.م";
    }
}
