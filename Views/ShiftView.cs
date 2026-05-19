using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة إدارة الورديات — TASK-023</summary>
internal class ShiftView : UserControl
{
    private readonly ShiftRepository    _repo      = new();
    private readonly TreasuryRepository _treasRepo = new();

    // ── لوحة الوردية الحالية ──────────────────────────────
    private Panel  _pnlCurrent  = null!;
    private Label  _lblStatus   = null!;
    private Label  _lblShiftNum = null!;
    private Label  _lblCashier  = null!;
    private Label  _lblOpened   = null!;
    private Label  _lblSales    = null!;
    private Label  _lblReturns  = null!;
    private Label  _lblExpenses = null!;
    private Label  _lblExpected = null!;
    private Button _btnOpen     = null!;
    private Button _btnClose    = null!;
    private Button _btnRefresh  = null!;

    // ── جدول سجل الورديات ─────────────────────────────────
    private DataGridView _grid    = null!;
    private Label        _lblStat = null!;

    private TreasuryShift? _current;

    public ShiftView()
    {
        InitializeComponent();
        LoadCurrentShift();
        LoadHistory();
    }

    private void InitializeComponent()
    {
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;

        // ── لوحة الوردية الحالية ──────────────────────────
        _pnlCurrent = AppTheme.CreateCard();
        _pnlCurrent.Dock   = DockStyle.Top;
        _pnlCurrent.Height = 210;
        _pnlCurrent.Padding = new Padding(16, 12, 16, 12);

        var lblTitle = new Label
        {
            Text = "📋 الوردية الحالية",
            Dock = DockStyle.Top, Height = 32,
            Font = AppTheme.SectionFont, ForeColor = AppTheme.Primary
        };

        // صف المعلومات
        var pnlInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 90,
            ColumnCount = 4, RowCount = 2,
            BackColor = AppTheme.Surface
        };
        for (int i = 0; i < 4; i++)
            pnlInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        _lblStatus   = MakeInfoLabel("الحالة:",     "—");
        _lblShiftNum = MakeInfoLabel("رقم الوردية:", "—");
        _lblCashier  = MakeInfoLabel("الكاشير:",    "—");
        _lblOpened   = MakeInfoLabel("وقت الفتح:",  "—");
        _lblSales    = MakeInfoLabel("المبيعات:",   "0.00");
        _lblReturns  = MakeInfoLabel("المرتجعات:",  "0.00");
        _lblExpenses = MakeInfoLabel("المصروفات:",  "0.00");
        _lblExpected = MakeInfoLabel("الرصيد المتوقع:", "0.00");

        AddInfoPair(pnlInfo, 0, 0, "الحالة:",          _lblStatus);
        AddInfoPair(pnlInfo, 1, 0, "رقم الوردية:",      _lblShiftNum);
        AddInfoPair(pnlInfo, 2, 0, "الكاشير:",          _lblCashier);
        AddInfoPair(pnlInfo, 3, 0, "وقت الفتح:",        _lblOpened);
        AddInfoPair(pnlInfo, 0, 1, "💰 المبيعات:",      _lblSales);
        AddInfoPair(pnlInfo, 1, 1, "🔄 المرتجعات:",     _lblReturns);
        AddInfoPair(pnlInfo, 2, 1, "💸 المصروفات:",     _lblExpenses);
        AddInfoPair(pnlInfo, 3, 1, "✅ الرصيد المتوقع:", _lblExpected);

        // أزرار
        var pnlBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 46,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = AppTheme.Surface
        };

        _btnOpen = new Button
        {
            Text = "🟢 فتح وردية جديدة", Width = 170, Height = 36,
            BackColor = AppTheme.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 4, 0, 0)
        };
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.Click += OnOpenShift;

        _btnClose = new Button
        {
            Text = "🔴 إغلاق الوردية", Width = 160, Height = 36,
            BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 4, 0, 0), Enabled = false
        };
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.Click += OnCloseShift;

        _btnRefresh = new Button
        {
            Text = "🔄 تحديث", Width = 100, Height = 36,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 4, 0, 0)
        };
        _btnRefresh.FlatAppearance.BorderColor = AppTheme.Border;
        _btnRefresh.Click += (_, _) => { LoadCurrentShift(); LoadHistory(); };

        pnlBtns.Controls.Add(_btnOpen);
        pnlBtns.Controls.Add(_btnClose);
        pnlBtns.Controls.Add(_btnRefresh);

        _pnlCurrent.Controls.Add(pnlBtns);
        _pnlCurrent.Controls.Add(pnlInfo);
        _pnlCurrent.Controls.Add(lblTitle);

        // ── جدول سجل الورديات ────────────────────────────
        _grid = new DataGridView
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Num",      HeaderText = "رقم الوردية",    Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cashier",  HeaderText = "الكاشير",         Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OpenedAt", HeaderText = "وقت الفتح",       Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClosedAt", HeaderText = "وقت الإغلاق",     Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Opening",  HeaderText = "رصيد الفتح",      Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Expected", HeaderText = "الرصيد المتوقع",  Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Actual",   HeaderText = "الرصيد الفعلي",   Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Diff",     HeaderText = "الفرق",           Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "الحالة",          Width = 100 });

        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) PrintReport(_grid.Rows[e.RowIndex].Tag as TreasuryShift); };

        _lblStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(8, 0, 8, 0)
        };

        Controls.Add(_grid);
        Controls.Add(_pnlCurrent);
        Controls.Add(_lblStat);
    }

    // ══ تحميل البيانات ═══════════════════════════════════════

    private void LoadCurrentShift()
    {
        try
        {
            _current = SessionContext.IsAdmin
                ? _repo.GetOpenShift()
                : _repo.GetOpenShiftForUser(SessionContext.CurrentUser!.Id);

            SessionContext.SetCurrentShift(_current);
            UpdateCurrentPanel();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل الوردية:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateCurrentPanel()
    {
        bool hasOpen = _current != null;
        _btnOpen.Enabled  = !hasOpen;
        _btnClose.Enabled = hasOpen;

        if (_current == null)
        {
            _lblStatus.Text   = "لا توجد وردية مفتوحة";
            _lblStatus.ForeColor = AppTheme.MutedText;
            _lblShiftNum.Text = "—";
            _lblCashier.Text  = "—";
            _lblOpened.Text   = "—";
            _lblSales.Text    = "—";
            _lblReturns.Text  = "—";
            _lblExpenses.Text = "—";
            _lblExpected.Text = "—";
            return;
        }

        _lblStatus.Text      = _current.StatusAr;
        _lblStatus.ForeColor = AppTheme.Success;
        _lblShiftNum.Text    = _current.ShiftNumber;
        _lblCashier.Text     = _current.CashierName;
        _lblOpened.Text      = _current.OpenedAt.ToString("yyyy-MM-dd HH:mm");

        try
        {
            var summary = _repo.GetShiftSummary(_current.Id);
            _lblSales.Text    = summary.TotalSales.ToString("N2")    + " ج.م";
            _lblReturns.Text  = summary.TotalReturns.ToString("N2")  + " ج.م";
            _lblExpenses.Text = summary.TotalExpenses.ToString("N2") + " ج.م";
            _lblExpected.Text = summary.ExpectedClosing.ToString("N2") + " ج.م";
            _lblExpected.ForeColor = AppTheme.Primary;
        }
        catch { /* الإحصائيات غير متاحة */ }
    }

    private void LoadHistory()
    {
        try
        {
            var shifts = SessionContext.IsAdmin
                ? _repo.GetAll()
                : _repo.GetAll(SessionContext.CurrentUser!.Id);

            _grid.Rows.Clear();
            foreach (var s in shifts)
            {
                int row = _grid.Rows.Add(
                    s.ShiftNumber,
                    s.CashierName,
                    s.OpenedAt.ToString("yyyy-MM-dd HH:mm"),
                    s.ClosedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—",
                    s.OpeningBalance.ToString("N2") + " ج.م",
                    s.ExpectedClosing.ToString("N2") + " ج.م",
                    s.ActualClosing.HasValue ? s.ActualClosing.Value.ToString("N2") + " ج.م" : "—",
                    FormatDiff(s.Difference),
                    s.StatusAr);
                _grid.Rows[row].Tag = s;

                if (s.IsOpen)
                    _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
                else if (s.Difference < 0)
                    _grid.Rows[row].DefaultCellStyle.ForeColor = AppTheme.Danger;
            }

            int open   = shifts.Count(x => x.IsOpen);
            int closed = shifts.Count(x => !x.IsOpen);
            _lblStat.Text = $"إجمالي الورديات: {shifts.Count}  |  🟢 مفتوحة: {open}  |  🔴 مغلقة: {closed}  |  (دبل كليك للتقرير)";
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل سجل الورديات:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string FormatDiff(decimal? diff)
    {
        if (!diff.HasValue) return "—";
        return diff.Value >= 0
            ? $"+{diff.Value:N2} ج.م"
            : $"{diff.Value:N2} ج.م";
    }

    // ══ فتح وردية ════════════════════════════════════════════

    private void OnOpenShift(object? sender, EventArgs e)
    {
        var safes = _treasRepo.GetSafes();
        using var dlg = new OpenShiftDialog(safes);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _current = _repo.OpenShift(
                SessionContext.CurrentUser!.Id,
                dlg.SafeId, dlg.OpeningBalance, dlg.Notes);
            SessionContext.SetCurrentShift(_current);
            UpdateCurrentPanel();
            LoadHistory();
            MessageBox.Show($"✅ تم فتح الوردية {_current.ShiftNumber} بنجاح.",
                "فتح وردية", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في فتح الوردية:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ══ إغلاق وردية ══════════════════════════════════════════

    private void OnCloseShift(object? sender, EventArgs e)
    {
        if (_current == null) return;

        var summary = _repo.GetShiftSummary(_current.Id);
        using var dlg = new CloseShiftDialog(_current, summary);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _repo.CloseShift(_current.Id, dlg.ActualClosing, dlg.Notes);
            var closed = _repo.GetById(_current.Id)!;
            SessionContext.SetCurrentShift(null);
            _current = null;
            UpdateCurrentPanel();
            LoadHistory();
            PrintReport(closed);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في إغلاق الوردية:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ══ طباعة التقرير ════════════════════════════════════════

    private void PrintReport(TreasuryShift? shift)
    {
        if (shift == null) return;
        ShiftReportDialog.Show(this, shift, _repo.GetShiftSummary(shift.Id));
    }

    // ── helpers ───────────────────────────────────────────────
    private static Label MakeInfoLabel(string caption, string val)
    {
        return new Label { Text = val, AutoSize = true, ForeColor = AppTheme.DarkText,
            Font = AppTheme.SectionFont };
    }

    private static void AddInfoPair(TableLayoutPanel tbl, int col, int row,
        string caption, Label valLabel)
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        pnl.Controls.Add(new Label { Text = caption, Dock = DockStyle.Top,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont });
        pnl.Controls.Add(valLabel);
        tbl.Controls.Add(pnl, col, row);
    }
}

// ══════════════════════════════════════════════════════════════
/// <summary>نموذج فتح وردية جديدة</summary>
internal class OpenShiftDialog : Form
{
    private ComboBox _cmbSafe   = null!;
    private TextBox  _txtBal    = null!;
    private TextBox  _txtNotes  = null!;
    private Button   _btnOk     = null!;
    private Button   _btnCancel = null!;

    private readonly List<Safe> _safes;

    public int?    SafeId         { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public string  Notes          { get; private set; } = "";

    public OpenShiftDialog(List<Safe> safes)
    {
        _safes = safes;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "🟢 فتح وردية جديدة";
        Size = new Size(400, 250);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false; MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;

        int y = 20;
        void Row(string lbl, Control ctrl)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(20, y + 3),
                AutoSize = true, ForeColor = AppTheme.DarkText });
            ctrl.Location = new Point(150, y); ctrl.Width = 210;
            Controls.Add(ctrl);
            y += 44;
        }

        _cmbSafe = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _cmbSafe.Items.Add(new SafeItem(0, "— بدون خزينة —"));
        foreach (var s in _safes) _cmbSafe.Items.Add(new SafeItem(s.Id, s.NameAr));
        _cmbSafe.SelectedIndex = _safes.Count > 0 ? 1 : 0;
        Row("الخزينة:", _cmbSafe);

        _txtBal = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, Text = "0" };
        _txtBal.KeyPress += (_, e) =>
        { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.') e.Handled = true; };
        Row("رصيد الفتح:", _txtBal);

        _txtNotes = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        Row("ملاحظات:", _txtNotes);

        _btnOk = new Button { Text = "✔ فتح", Location = new Point(150, y), Size = new Size(100, 34),
            BackColor = AppTheme.Success, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnOk.FlatAppearance.BorderSize = 0;
        _btnOk.Click += (_, _) =>
        {
            if (!decimal.TryParse(_txtBal.Text.Trim(), out var bal) || bal < 0)
            { MessageBox.Show("أدخل رصيد فتح صحيح.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            SafeId = (_cmbSafe.SelectedItem as SafeItem)?.Id is int sid && sid > 0 ? sid : null;
            OpeningBalance = bal;
            Notes = _txtNotes.Text.Trim();
            DialogResult = DialogResult.OK; Close();
        };

        _btnCancel = new Button { Text = "إلغاء", Location = new Point(260, y), Size = new Size(100, 34),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat };
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(_btnOk); Controls.Add(_btnCancel);
        ClientSize = new Size(400, y + 55);
    }
    private record SafeItem(int Id, string Label) { public override string ToString() => Label; }
}

// ══════════════════════════════════════════════════════════════
/// <summary>نموذج إغلاق الوردية</summary>
internal class CloseShiftDialog : Form
{
    private TextBox  _txtActual  = null!;
    private TextBox  _txtNotes   = null!;
    private Label    _lblDiff    = null!;
    private Button   _btnOk      = null!;
    private Button   _btnCancel  = null!;

    private readonly TreasuryShift _shift;
    private readonly ShiftSummary  _summary;

    public decimal ActualClosing { get; private set; }
    public string  Notes         { get; private set; } = "";

    public CloseShiftDialog(TreasuryShift shift, ShiftSummary summary)
    {
        _shift   = shift;
        _summary = summary;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "🔴 إغلاق الوردية";
        Size = new Size(420, 380);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false; MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = AppTheme.Background;
        Font = AppTheme.BodyFont;

        // ملخص
        var pnlSummary = new Panel { Location = new Point(20, 16), Size = new Size(360, 170),
            BackColor = AppTheme.Surface, BorderStyle = BorderStyle.FixedSingle };

        void SumRow(string lbl, string val, int y, bool bold = false)
        {
            pnlSummary.Controls.Add(new Label { Text = lbl, Location = new Point(10, y),
                AutoSize = true, ForeColor = AppTheme.MutedText });
            pnlSummary.Controls.Add(new Label { Text = val, Location = new Point(220, y),
                AutoSize = true, ForeColor = AppTheme.DarkText,
                Font = bold ? AppTheme.SectionFont : AppTheme.BodyFont });
        }

        SumRow("رصيد الفتح:",         _summary.OpeningBalance.ToString("N2")  + " ج.م", 10);
        SumRow("إجمالي المبيعات:",     _summary.TotalSales.ToString("N2")     + " ج.م", 35);
        SumRow("مرتجعات المبيعات:",   (-_summary.TotalReturns).ToString("N2") + " ج.م", 60);
        SumRow("المصروفات النقدية:",   (-_summary.TotalExpenses).ToString("N2")+ " ج.م", 85);
        pnlSummary.Controls.Add(new Label { Text = new string('─', 42),
            Location = new Point(10, 110), AutoSize = true, ForeColor = AppTheme.Border });
        SumRow("الرصيد المتوقع:",      _summary.ExpectedClosing.ToString("N2") + " ج.م", 120, true);

        Controls.Add(pnlSummary);

        int y = 205;
        void Row(string lbl, Control ctrl)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(20, y + 3),
                AutoSize = true, ForeColor = AppTheme.DarkText });
            ctrl.Location = new Point(170, y); ctrl.Width = 210;
            Controls.Add(ctrl); y += 44;
        }

        _txtActual = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
            Text = _summary.ExpectedClosing.ToString("N2") };
        _txtActual.TextChanged += (_, _) => UpdateDiff();
        Row("الرصيد الفعلي (عد):", _txtActual);

        _lblDiff = new Label { Text = "الفرق: —", Location = new Point(170, y - 30),
            AutoSize = true, Font = AppTheme.SectionFont };
        Controls.Add(_lblDiff);

        _txtNotes = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        Row("ملاحظات:", _txtNotes);

        _btnOk = new Button { Text = "✔ إغلاق وطباعة", Location = new Point(20, y), Size = new Size(150, 34),
            BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnOk.FlatAppearance.BorderSize = 0;
        _btnOk.Click += (_, _) =>
        {
            if (!decimal.TryParse(_txtActual.Text.Trim(), out var actual) || actual < 0)
            { MessageBox.Show("أدخل الرصيد الفعلي.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            ActualClosing = actual;
            Notes = _txtNotes.Text.Trim();
            DialogResult = DialogResult.OK; Close();
        };
        _btnCancel = new Button { Text = "إلغاء", Location = new Point(180, y), Size = new Size(100, 34),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat };
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(_btnOk); Controls.Add(_btnCancel);
        ClientSize = new Size(420, y + 60);
        UpdateDiff();
    }

    private void UpdateDiff()
    {
        if (!decimal.TryParse(_txtActual.Text.Trim(), out var actual)) { _lblDiff.Text = "الفرق: —"; return; }
        decimal diff = actual - _summary.ExpectedClosing;
        _lblDiff.Text      = diff >= 0 ? $"الفرق: +{diff:N2} ج.م (زيادة)" : $"الفرق: {diff:N2} ج.م (عجز)";
        _lblDiff.ForeColor = diff >= 0 ? AppTheme.Success : AppTheme.Danger;
    }
}

// ══════════════════════════════════════════════════════════════
/// <summary>تقرير الوردية — قابل للطباعة</summary>
internal static class ShiftReportDialog
{
    public static void Show(IWin32Window owner, TreasuryShift shift, ShiftSummary summary)
    {
        var frm = new Form
        {
            Text = $"تقرير الوردية {shift.ShiftNumber}",
            Size = new Size(480, 500),
            StartPosition = FormStartPosition.CenterParent,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true,
            BackColor = Color.White,
            Font = new Font("Courier New", 10F)
        };

        decimal diff    = (shift.Difference ?? 0);
        string diffText = diff >= 0 ? $"+{diff:N2} (زيادة)" : $"{diff:N2} (عجز)";

        string report = $"""
┌─────────────────────────────────────────┐
│  تقرير إغلاق الوردية                   │
│  الرقم: {shift.ShiftNumber,-33}│
│  الكاشير: {shift.CashierName,-31}│
│  الفتح:  {shift.OpenedAt:yyyy-MM-dd HH:mm}                    │
│  الإغلاق:{shift.ClosedAt?.ToString("yyyy-MM-dd HH:mm") ?? "—",25}│
├─────────────────────────────────────────┤
│  رصيد فتح الوردية:{summary.OpeningBalance,15:N2} ج.م  │
│  إجمالي المبيعات: {summary.TotalSales,15:N2} ج.م  │
│  مرتجعات المبيعات:{-summary.TotalReturns,15:N2} ج.م  │
│  المصروفات النقدية:{-summary.TotalExpenses,14:N2} ج.م  │
│                          ─────────────  │
│  الرصيد المتوقع:  {summary.ExpectedClosing,15:N2} ج.م  │
│  الرصيد الفعلي:   {shift.ActualClosing ?? 0,15:N2} ج.م  │
│  الفرق:           {diffText,22}  │
└─────────────────────────────────────────┘
""";

        var txt = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, Text = report, Font = new Font("Courier New", 10F),
            BackColor = Color.White
        };

        var btnPrint = new Button
        {
            Text = "🖨️ طباعة", Dock = DockStyle.Bottom, Height = 40,
            BackColor = AppTheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.Click += (_, _) =>
        {
            var pd = new System.Drawing.Printing.PrintDocument();
            pd.PrintPage += (s, e) =>
            {
                e.Graphics!.DrawString(report, new Font("Courier New", 10F),
                    Brushes.Black, e.MarginBounds);
            };
            new System.Windows.Forms.PrintDialog { Document = pd }.ShowDialog(frm);
            pd.Print();
        };

        frm.Controls.Add(txt);
        frm.Controls.Add(btnPrint);
        frm.ShowDialog(owner);
    }
}
