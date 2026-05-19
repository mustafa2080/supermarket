using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة إدارة سجلات التالف والمُهلَك — TASK-021</summary>
internal class DamageView : UserControl
{
    private readonly WarehouseRepository _repo = new();

    private Button       _btnNew    = null!;
    private Button       _btnRefresh= null!;
    private DataGridView _grid      = null!;
    private Button       _btnOpen   = null!;
    private Label        _lblStatus = null!;

    private List<Warehouse>     _warehouses = new();
    private List<DamageRecord>  _records    = new();

    public DamageView()
    {
        InitializeComponent();
        LoadWarehouses();
        LoadRecords();
    }

    private void InitializeComponent()
    {
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;

        // ── شريط الأدوات ──────────────────────────────────────
        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(10, 10, 10, 10)
        };

        _btnNew = new Button
        {
            Text      = "➕ سجل تالف جديد",
            Width     = 160,
            Dock      = DockStyle.Right,
            Margin    = new Padding(0, 0, 8, 0),
            BackColor = AppTheme.Danger,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnNew.FlatAppearance.BorderSize = 0;
        _btnNew.Click += OnNew;

        _btnRefresh = new Button
        {
            Text      = "🔄 تحديث",
            Width     = 100,
            Dock      = DockStyle.Right,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.DarkText,
            FlatStyle = FlatStyle.Flat
        };
        _btnRefresh.FlatAppearance.BorderColor = AppTheme.Border;
        _btnRefresh.Click += (_, _) => LoadRecords();

        toolbar.Controls.Add(_btnNew);
        toolbar.Controls.Add(_btnRefresh);

        // ── جدول السجلات ──────────────────────────────────────
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
            RowTemplate           = { Height = 34 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight   = 40,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Danger,
                ForeColor = Color.White,
                Font      = AppTheme.SectionFont,
                Padding   = new Padding(6)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor            = AppTheme.Surface,
                ForeColor            = AppTheme.DarkText,
                Padding              = new Padding(4),
                SelectionBackColor   = Color.FromArgb(255, 205, 210),
                SelectionForeColor   = AppTheme.DarkText
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Background }
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Num",        HeaderText = "رقم السجل",      Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",       HeaderText = "التاريخ",         Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Warehouse",  HeaderText = "المستودع",        Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason",     HeaderText = "سبب التلف",       Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalValue", HeaderText = "قيمة التلف",      Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",     HeaderText = "الحالة",          Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy",  HeaderText = "أنشأه",           Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ApprovedBy", HeaderText = "اعتمده",          Width = 130 });

        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };

        // ── شريط الحالة ───────────────────────────────────────
        var bottomBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 38,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(10, 8, 10, 4)
        };

        _btnOpen = new Button
        {
            Text      = "📂 فتح",
            Width     = 100,
            Dock      = DockStyle.Right,
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.Click += (_, _) => OpenSelected();

        _lblStatus = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = AppTheme.MutedText,
            Font      = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Text      = "جارٍ التحميل..."
        };

        bottomBar.Controls.Add(_btnOpen);
        bottomBar.Controls.Add(_lblStatus);

        Controls.Add(_grid);
        Controls.Add(toolbar);
        Controls.Add(bottomBar);
    }

    // ══════════════════════════════════════════════════════════
    private void LoadWarehouses()
    {
        try { _warehouses = _repo.GetAll(); }
        catch { /* نكمل بدون فلتر */ }
    }

    private void LoadRecords()
    {
        try
        {
            _records = _repo.GetDamageRecords();
            BindGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل سجلات التالف:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid()
    {
        _grid.Rows.Clear();
        int pending  = 0;
        int approved = 0;
        decimal totalVal = 0;

        foreach (var r in _records)
        {
            var statusAr = r.Status switch
            {
                "pending"  => "⏳ بانتظار الاعتماد",
                "approved" => "✅ معتمد",
                "rejected" => "❌ مرفوض",
                _          => r.Status
            };
            var reasonAr = r.Reason switch
            {
                "damage"  => "تلف",
                "expired" => "انتهاء صلاحية",
                "theft"   => "سرقة",
                _         => r.Reason
            };

            int row = _grid.Rows.Add(
                r.RecordNumber,
                r.RecordDate.ToString("yyyy-MM-dd"),
                r.WarehouseName,
                reasonAr,
                r.TotalValue.ToString("N2") + " ج.م",
                statusAr,
                r.CreatedBy,
                r.ApprovedBy
            );
            _grid.Rows[row].Tag = r;

            // تلوين الصفوف
            if (r.Status == "approved")
                _grid.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
            else if (r.Status == "pending")
                _grid.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 231);

            if (r.Status == "pending")  pending++;
            if (r.Status == "approved") approved++;
            totalVal += r.TotalValue;
        }

        _lblStatus.Text = $"الإجمالي: {_records.Count} سجل  |  " +
                          $"⏳ بانتظار الاعتماد: {pending}  |  " +
                          $"✅ معتمد: {approved}  |  " +
                          $"إجمالي القيم: {totalVal:N2} ج.م";
    }

    // ── سجل جديد ──────────────────────────────────────────────
    private void OnNew(object? sender, EventArgs e)
    {
        using var dlg = new NewDamageSetupDialog(_warehouses);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            int id = _repo.CreateDamageRecord(
                dlg.WarehouseId, dlg.Reason, dlg.Notes,
                SessionContext.CurrentUser!.Id);

            var editDlg = new DamageDialog(id);
            editDlg.ShowDialog(this);
            LoadRecords();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في إنشاء السجل:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── فتح سجل موجود ─────────────────────────────────────────
    private void OpenSelected()
    {
        if (_grid.CurrentRow?.Tag is not DamageRecord rec) return;
        bool readOnly = rec.Status == "approved";
        using var dlg = new DamageDialog(rec.Id, readOnly);
        if (dlg.ShowDialog(this) == DialogResult.OK || !readOnly)
            LoadRecords();
    }
}

// ══════════════════════════════════════════════════════════════════════
/// <summary>نموذج اختيار المستودع وسبب التلف قبل إنشاء السجل</summary>
internal class NewDamageSetupDialog : Form
{
    private ComboBox _cmbWH     = null!;
    private ComboBox _cmbReason = null!;
    private TextBox  _txtNotes  = null!;
    private Button   _btnOk     = null!;
    private Button   _btnCancel = null!;

    private readonly List<Warehouse> _warehouses;

    public int    WarehouseId { get; private set; }
    public string Reason      { get; private set; } = "damage";
    public string Notes       { get; private set; } = "";

    public NewDamageSetupDialog(List<Warehouse> warehouses)
    {
        _warehouses = warehouses;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text              = "🗑️ إنشاء سجل تالف جديد";
        Size              = new Size(420, 280);
        StartPosition     = FormStartPosition.CenterParent;
        MinimizeBox       = false; MaximizeBox = false;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;

        int y = 20;
        void AddRow(string label, Control ctrl)
        {
            Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true,
                ForeColor = AppTheme.DarkText });
            ctrl.Location = new Point(140, y); ctrl.Width = 240;
            Controls.Add(ctrl);
            y += 42;
        }

        _cmbWH = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        foreach (var w in _warehouses)
            _cmbWH.Items.Add(new ComboItem(w.Id, w.Name));
        if (_cmbWH.Items.Count > 0) _cmbWH.SelectedIndex = 0;
        AddRow("المستودع:", _cmbWH);

        _cmbReason = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _cmbReason.Items.Add(new ComboItemEx(0, "تلف",           "damage"));
        _cmbReason.Items.Add(new ComboItemEx(0, "انتهاء صلاحية", "expired"));
        _cmbReason.Items.Add(new ComboItemEx(0, "سرقة",          "theft"));
        _cmbReason.SelectedIndex = 0;
        AddRow("سبب التلف:", _cmbReason);

        _txtNotes = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        AddRow("ملاحظات:", _txtNotes);

        _btnOk = new Button
        {
            Text = "✔ إنشاء", Location = new Point(140, y), Size = new Size(110, 34),
            BackColor = AppTheme.Danger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        _btnOk.FlatAppearance.BorderSize = 0;
        _btnOk.Click += OnOk;

        _btnCancel = new Button
        {
            Text = "إلغاء", Location = new Point(270, y), Size = new Size(110, 34),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat
        };
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (_cmbWH.SelectedItem is not ComboItem wh)
        {
            MessageBox.Show("اختر المستودع.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        WarehouseId = wh.Id;
        Reason      = (_cmbReason.SelectedItem as ComboItemEx)?.Value ?? "damage";
        Notes       = _txtNotes.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }

    // helpers
    private record ComboItem(int Id, string Label)
    {
        public override string ToString() => Label;
    }
    private record ComboItemEx(int Id, string Label, string Value)
    {
        public override string ToString() => Label;
    }
}
