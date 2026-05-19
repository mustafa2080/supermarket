using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة التسعير متعدد المستويات — TASK-024</summary>
internal class PricingView : UserControl
{
    private readonly PricingRepository _repo    = new();
    private readonly ItemRepository    _itemRepo = new();

    private TabControl _tabs      = null!;
    private TabPage    _tabBulk   = null!;
    private TabPage    _tabHist   = null!;

    // ── تاب التحديث الجماعي ──────────────────────────────────
    private RadioButton _rbRetail   = null!;
    private RadioButton _rbWhole    = null!;
    private RadioButton _rbPurchase = null!;

    private RadioButton _rbPctUp    = null!;
    private RadioButton _rbPctDown  = null!;
    private RadioButton _rbAmtUp    = null!;
    private RadioButton _rbAmtDown  = null!;
    private RadioButton _rbFixed    = null!;

    private ComboBox _cmbGroup    = null!;
    private ComboBox _cmbSupplier = null!;
    private TextBox  _txtValue    = null!;
    private TextBox  _txtReason   = null!;

    private DataGridView _gridPreview = null!;
    private Label        _lblPreview  = null!;
    private Button       _btnPreview  = null!;
    private Button       _btnApply    = null!;

    private List<BulkPricePreviewRow> _previewRows = new();

    // ── تاب تاريخ الأسعار ─────────────────────────────────────
    private DataGridView _gridHist   = null!;
    private Label        _lblHist    = null!;
    private ComboBox     _cmbHType   = null!;
    private DateTimePicker _dtFrom   = null!;
    private DateTimePicker _dtTo     = null!;
    private TextBox        _txtSearch = null!;
    private List<PriceHistoryEntry> _histRows = new();

    // بيانات مساعدة
    private List<ItemGroup> _groups    = new();
    private List<Supplier>  _suppliers = new();

    public PricingView()
    {
        InitializeComponent();
        LoadMasterData();
    }

    private void InitializeComponent()
    {
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
        _tabBulk = new TabPage("⚡ تحديث جماعي للأسعار") { BackColor = AppTheme.Surface };
        _tabHist = new TabPage("📜 سجل تغييرات الأسعار") { BackColor = AppTheme.Surface };

        _tabBulk.Controls.Add(BuildBulkPanel());
        _tabHist.Controls.Add(BuildHistPanel());

        _tabs.TabPages.Add(_tabBulk);
        _tabs.TabPages.Add(_tabHist);
        Controls.Add(_tabs);
    }

    // ════ تاب التحديث الجماعي ════════════════════════════════

    private Control BuildBulkPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        // ─ شريط الإعدادات ────────────────────────────────────
        var settings = AppTheme.CreateCard();
        settings.Dock    = DockStyle.Top;
        settings.Height  = 200;
        settings.Padding = new Padding(14, 10, 14, 10);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
            BackColor = AppTheme.Surface
        };
        for (int i = 0; i < 4; i++)
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        // صف 1 — نوع السعر
        var grpType = new GroupBox { Text = "نوع السعر", Dock = DockStyle.Fill,
            ForeColor = AppTheme.Primary, Font = AppTheme.SmallFont };
        _rbRetail   = MakeRb("تجزئة",    true);
        _rbWhole    = MakeRb("جملة",     false);
        _rbPurchase = MakeRb("شراء",     false);
        var fType = new FlowLayoutPanel { Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft };
        fType.Controls.AddRange(new Control[] { _rbRetail, _rbWhole, _rbPurchase });
        grpType.Controls.Add(fType);

        // صف 1 — طريقة التحديث
        var grpMethod = new GroupBox { Text = "طريقة التحديث", Dock = DockStyle.Fill,
            ForeColor = AppTheme.Primary, Font = AppTheme.SmallFont };
        _rbPctUp   = MakeRb("رفع %",   true);
        _rbPctDown = MakeRb("خفض %",   false);
        _rbAmtUp   = MakeRb("إضافة مبلغ", false);
        _rbAmtDown = MakeRb("خصم مبلغ",   false);
        _rbFixed   = MakeRb("سعر ثابت",   false);
        var fMeth = new FlowLayoutPanel { Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft };
        fMeth.Controls.AddRange(new Control[]
            { _rbPctUp, _rbPctDown, _rbAmtUp, _rbAmtDown, _rbFixed });
        grpMethod.Controls.Add(fMeth);

        // صف 1 — تطبيق على
        var grpOn = new GroupBox { Text = "تطبيق على", Dock = DockStyle.Fill,
            ForeColor = AppTheme.Primary, Font = AppTheme.SmallFont };
        _cmbGroup    = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface };
        _cmbSupplier = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface };
        grpOn.Controls.Add(_cmbSupplier);
        grpOn.Controls.Add(new Label { Text = "مورد:", Dock = DockStyle.Top,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont });
        grpOn.Controls.Add(_cmbGroup);
        grpOn.Controls.Add(new Label { Text = "مجموعة:", Dock = DockStyle.Top,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont });

        // صف 1 — القيمة + السبب
        var grpVal = new GroupBox { Text = "القيمة", Dock = DockStyle.Fill,
            ForeColor = AppTheme.Primary, Font = AppTheme.SmallFont };
        _txtValue = new TextBox { Dock = DockStyle.Top, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.DarkText, Text = "0" };
        _txtValue.KeyPress += (_, e) =>
        { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
              e.Handled = true; };
        _txtReason = new TextBox { Dock = DockStyle.Top, BackColor = AppTheme.Surface,
            ForeColor = AppTheme.MutedText, Text = "" };
        grpVal.Controls.Add(_txtReason);
        grpVal.Controls.Add(new Label { Text = "السبب (اختياري):", Dock = DockStyle.Top,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont });
        grpVal.Controls.Add(_txtValue);
        grpVal.Controls.Add(new Label { Text = "القيمة:", Dock = DockStyle.Top,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont });

        tbl.Controls.Add(grpType,   0, 0);
        tbl.Controls.Add(grpMethod, 1, 0);
        tbl.Controls.Add(grpOn,     2, 0);
        tbl.Controls.Add(grpVal,    3, 0);

        // أزرار
        var pnlBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = AppTheme.Surface, Padding = new Padding(8, 8, 8, 8)
        };
        _btnPreview = MakeBtn("🔍 معاينة", AppTheme.Primary, OnPreview);
        _btnApply   = MakeBtn("✅ تطبيق",  AppTheme.Success, OnApply);
        _btnApply.Enabled = false;
        pnlBtns.Controls.Add(_btnPreview);
        pnlBtns.Controls.Add(_btnApply);

        settings.Controls.Add(pnlBtns);
        settings.Controls.Add(tbl);

        // ─ جدول المعاينة ─────────────────────────────────────
        _gridPreview = BuildGrid(new[]
        {
            ("Sel",      "✔",         36),
            ("Code",     "الكود",     100),
            ("Name",     "اسم الصنف", 220),
            ("Group",    "المجموعة",  120),
            ("CurPrice", "السعر الحالي", 110),
            ("NewPrice", "السعر الجديد", 110),
            ("Change",   "التغيير",       90),
            ("ChangePct","نسبة %",         80),
        });
        _gridPreview.Columns["Sel"]!.ReadOnly = false;
        (_gridPreview.Columns["Sel"] as DataGridViewCheckBoxColumn)?.ToString();
        // نستبدل عمود Sel بـ CheckBox
        _gridPreview.Columns.Remove("Sel");
        _gridPreview.Columns.Insert(0, new DataGridViewCheckBoxColumn
        {
            Name = "Sel", HeaderText = "✔", Width = 40,
            ReadOnly = false, TrueValue = true, FalseValue = false
        });

        _lblPreview = new Label
        {
            Dock = DockStyle.Bottom, Height = 28,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(8, 0, 8, 0),
            Text = "اضغط «معاينة» لعرض الأصناف المتأثرة"
        };

        pnl.Controls.Add(_gridPreview);
        pnl.Controls.Add(_lblPreview);
        pnl.Controls.Add(settings);
        return pnl;
    }

    // ════ تاب تاريخ الأسعار ══════════════════════════════════

    private Control BuildHistPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        // شريط الفلتر
        var toolbar = new Panel
        {
            Dock = DockStyle.Top, Height = 52,
            BackColor = AppTheme.Surface, Padding = new Padding(10, 10, 10, 10)
        };

        _cmbHType = new ComboBox
        {
            Width = 120, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, Dock = DockStyle.Right
        };
        _cmbHType.Items.AddRange(new object[]
            { "الكل", "تجزئة", "جملة", "شراء" });
        _cmbHType.SelectedIndex = 0;

        _dtFrom = new DateTimePicker { Width = 120, Dock = DockStyle.Right,
            Value = DateTime.Today.AddMonths(-1), Format = DateTimePickerFormat.Short };
        _dtTo   = new DateTimePicker { Width = 120, Dock = DockStyle.Right,
            Value = DateTime.Today, Format = DateTimePickerFormat.Short };

        _txtSearch = new TextBox { Width = 160, Dock = DockStyle.Right,
            PlaceholderText = "بحث بالاسم / الكود...",
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _txtSearch.TextChanged += (_, _) => FilterHistory();

        var btnLoad = MakeBtn("🔍 بحث", AppTheme.Primary, (_, _) => LoadHistory());
        btnLoad.Dock = DockStyle.Right;
        btnLoad.Width = 90;

        toolbar.Controls.AddRange(new Control[]
            { btnLoad, _txtSearch,
              new Label { Text="إلى:", Dock=DockStyle.Right, AutoSize=true,
                          ForeColor=AppTheme.MutedText, TextAlign=ContentAlignment.MiddleRight },
              _dtTo,
              new Label { Text="من:", Dock=DockStyle.Right, AutoSize=true,
                          ForeColor=AppTheme.MutedText, TextAlign=ContentAlignment.MiddleRight },
              _dtFrom,
              new Label { Text="النوع:", Dock=DockStyle.Right, AutoSize=true,
                          ForeColor=AppTheme.MutedText, TextAlign=ContentAlignment.MiddleRight },
              _cmbHType });

        _gridHist = BuildGrid(new[]
        {
            ("Code",    "الكود",         100),
            ("Name",    "اسم الصنف",     220),
            ("Type",    "نوع السعر",      90),
            ("Old",     "السعر القديم",  110),
            ("New",     "السعر الجديد",  110),
            ("Change",  "التغيير",        90),
            ("Pct",     "نسبة %",         80),
            ("By",      "بواسطة",        120),
            ("At",      "التاريخ",       140),
            ("Reason",  "السبب",         180),
        });

        _lblHist = new Label
        {
            Dock = DockStyle.Bottom, Height = 28,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(8, 0, 8, 0)
        };

        pnl.Controls.Add(_gridHist);
        pnl.Controls.Add(_lblHist);
        pnl.Controls.Add(toolbar);
        return pnl;
    }

    // ════ تحميل البيانات الأساسية ════════════════════════════

    private void LoadMasterData()
    {
        try
        {
            _groups    = _itemRepo.GetGroups();
            _suppliers = _itemRepo.GetSuppliers();

            _cmbGroup.Items.Clear();
            _cmbGroup.Items.Add(new IdNameItem(0, "— الكل —"));
            foreach (var g in _groups)
                _cmbGroup.Items.Add(new IdNameItem(g.Id, g.NameAr));
            _cmbGroup.SelectedIndex = 0;

            _cmbSupplier.Items.Clear();
            _cmbSupplier.Items.Add(new IdNameItem(0, "— الكل —"));
            foreach (var s in _suppliers)
                _cmbSupplier.Items.Add(new IdNameItem(s.Id, s.Name));
            _cmbSupplier.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل البيانات:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ════ منطق التحديث الجماعي ═══════════════════════════════

    private void OnPreview(object? s, EventArgs e)
    {
        if (!decimal.TryParse(_txtValue.Text.Trim(), out var val) || val < 0)
        {
            MessageBox.Show("أدخل قيمة صحيحة.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var req = BuildRequest(val);
        try
        {
            _previewRows = _repo.PreviewBulkUpdate(req);
            BindPreview();
            _btnApply.Enabled = _previewRows.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في المعاينة:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindPreview()
    {
        _gridPreview.Rows.Clear();
        foreach (var r in _previewRows)
        {
            int row = _gridPreview.Rows.Add(
                r.Selected,
                r.ItemCode, r.ItemName, r.GroupName,
                r.CurrentPrice.ToString("N2") + " ج.م",
                r.NewPrice.ToString("N2")     + " ج.م",
                FormatChange(r.Change),
                r.ChangePct.ToString("N1") + "%");
            _gridPreview.Rows[row].Tag = r;
            if (r.Change > 0)
                _gridPreview.Rows[row].Cells["NewPrice"].Style.ForeColor = AppTheme.Danger;
            else if (r.Change < 0)
                _gridPreview.Rows[row].Cells["NewPrice"].Style.ForeColor = AppTheme.Success;
        }
        _lblPreview.Text =
            $"إجمالي الأصناف: {_previewRows.Count}  |  دبل كليك لتحديد/إلغاء الكل";
    }

    private void OnApply(object? s, EventArgs e)
    {
        // اجمع الأصناف المحددة
        var selected = new List<int>();
        foreach (DataGridViewRow row in _gridPreview.Rows)
        {
            if (row.Cells["Sel"].Value is true && row.Tag is BulkPricePreviewRow r)
                selected.Add(r.ItemId);
        }

        if (selected.Count == 0)
        {
            MessageBox.Show("لا توجد أصناف محددة.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"سيتم تحديث أسعار {selected.Count} صنف.\nهل أنت متأكد؟",
            "تأكيد التطبيق", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        if (!decimal.TryParse(_txtValue.Text.Trim(), out var val)) return;
        var req = BuildRequest(val);
        req.ItemIds = selected;

        try
        {
            int done = _repo.ApplyBulkUpdate(req);
            MessageBox.Show($"✅ تم تحديث {done} صنف بنجاح.",
                "اكتمل", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _previewRows.Clear();
            _gridPreview.Rows.Clear();
            _lblPreview.Text = "تم التطبيق. اضغط «معاينة» لتحديث آخر.";
            _btnApply.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في التطبيق:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private BulkPriceUpdateRequest BuildRequest(decimal val) => new()
    {
        PriceType  = _rbRetail.Checked   ? "retail"
                   : _rbWhole.Checked    ? "wholesale"
                   :                       "purchase",
        Method     = _rbPctUp.Checked    ? "percent_up"
                   : _rbPctDown.Checked  ? "percent_down"
                   : _rbAmtUp.Checked    ? "amount_up"
                   : _rbAmtDown.Checked  ? "amount_down"
                   :                       "fixed",
        Value      = val,
        GroupId    = (_cmbGroup.SelectedItem    as IdNameItem)?.Id is int gid && gid > 0 ? gid : null,
        SupplierId = (_cmbSupplier.SelectedItem as IdNameItem)?.Id is int sid && sid > 0 ? sid : null,
        Reason     = _txtReason.Text.Trim()
    };

    // ════ تاريخ الأسعار ══════════════════════════════════════

    private void LoadHistory()
    {
        try
        {
            var typeFilter = _cmbHType.SelectedIndex switch
            {
                1 => "retail",
                2 => "wholesale",
                3 => "purchase",
                _ => (string?)null
            };
            _histRows = _repo.GetHistory(null, typeFilter, _dtFrom.Value, _dtTo.Value);
            FilterHistory();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل التاريخ:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterHistory()
    {
        var q = _txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(q)
            ? _histRows
            : _histRows.Where(r =>
                r.ItemName.ToLower().Contains(q) ||
                r.ItemCode.ToLower().Contains(q)).ToList();

        _gridHist.Rows.Clear();
        foreach (var h in filtered)
        {
            int row = _gridHist.Rows.Add(
                h.ItemCode, h.ItemName, h.PriceTypeAr,
                h.OldPrice.ToString("N2") + " ج.م",
                h.NewPrice.ToString("N2") + " ج.م",
                FormatChange(h.Change),
                h.ChangePct.ToString("N1") + "%",
                h.ChangedBy,
                h.ChangedAt.ToString("yyyy-MM-dd HH:mm"),
                h.Reason);
            if (h.Change > 0)
                _gridHist.Rows[row].Cells["New"].Style.ForeColor = AppTheme.Danger;
            else if (h.Change < 0)
                _gridHist.Rows[row].Cells["New"].Style.ForeColor = AppTheme.Success;
        }
        _lblHist.Text =
            $"إجمالي السجلات: {filtered.Count}  " +
            $"|  ارتفاع: {filtered.Count(x => x.Change > 0)}  " +
            $"|  انخفاض: {filtered.Count(x => x.Change < 0)}";
    }

    // ── مساعدات UI ────────────────────────────────────────────

    private static DataGridView BuildGrid((string name, string header, int width)[] cols)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface,
            GridColor = AppTheme.Border, BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate = { Height = 33 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
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
            g.Columns.Add(new DataGridViewTextBoxColumn
                { Name = name, HeaderText = header, Width = width });
        return g;
    }

    private static RadioButton MakeRb(string text, bool check) =>
        new() { Text = text, AutoSize = true, Checked = check,
                ForeColor = AppTheme.DarkText, Font = AppTheme.SmallFont,
                Margin = new Padding(4, 4, 0, 0) };

    private static Button MakeBtn(string text, Color color, EventHandler handler)
    {
        var b = new Button
        {
            Text = text, Height = 34, Width = 150,
            BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 0, 0, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += handler;
        return b;
    }

    private static string FormatChange(decimal change) =>
        change >= 0 ? $"+{change:N2} ج.م" : $"{change:N2} ج.م";

    private record IdNameItem(int Id, string Label) { public override string ToString() => Label; }
}
