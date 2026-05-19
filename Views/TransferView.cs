using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة إدارة تحويلات المستودعات — TASK-020</summary>
internal class TransferView : UserControl
{
    private readonly WarehouseRepository _repo = new();

    private Label        _lblTitle  = null!;
    private Button       _btnNew    = null!;
    private Button       _btnRefresh= null!;
    private DataGridView _grid      = null!;
    private Button       _btnOpen   = null!;
    private Label        _lblStatus = null!;

    private List<Warehouse>       _warehouses = new();
    private List<WarehouseTransfer> _transfers  = new();

    public TransferView()
    {
        InitializeComponent();
        LoadWarehouses();
        LoadTransfers();
    }

    private void InitializeComponent()
    {
        Dock              = DockStyle.Fill;
        BackColor         = AppTheme.Background;
        RightToLeft = RightToLeft.Yes;
        Font        = AppTheme.BodyFont;

        _lblTitle = new Label
        {
            Text      = "🔄 تحويلات المستودعات",
            Font      = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary,
            AutoSize  = true,
            Location  = new Point(20, 20)
        };

        _btnNew = new Button
        {
            Text      = "➕ تحويل جديد",
            Location  = new Point(20, 68),
            Size      = new Size(140, 34),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnNew.FlatAppearance.BorderSize = 0;
        _btnNew.Click += OnNew;

        _btnRefresh = new Button
        {
            Text      = "🔄",
            Location  = new Point(175, 68),
            Size      = new Size(40, 34),
            BackColor = AppTheme.Surface,
            FlatStyle = FlatStyle.Flat,
            ForeColor = AppTheme.DarkText
        };
        _btnRefresh.FlatAppearance.BorderColor = AppTheme.Border;
        _btnRefresh.Click += (_, _) => LoadTransfers();

        _grid = new DataGridView
        {
            Location              = new Point(20, 115),
            Size                  = new Size(860, 470),
            BackgroundColor       = AppTheme.Surface,
            GridColor             = AppTheme.Border,
            BorderStyle           = BorderStyle.None,
            RowHeadersVisible     = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            ReadOnly              = true,
            SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Num",    HeaderText = "رقم التحويل",  Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "From",   HeaderText = "من مستودع",   Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "To",     HeaderText = "إلى مستودع",  Width = 170 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",   HeaderText = "التاريخ",      Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة",       Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "By",     HeaderText = "أنشأه",        Width = 130 });
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenTransfer(); };

        _btnOpen = new Button
        {
            Text      = "📂 فتح / تعديل",
            Location  = new Point(20, 600),
            Size      = new Size(150, 36),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.Click += (_, _) => OpenTransfer();

        _lblStatus = new Label
        {
            Text      = "",
            AutoSize  = true,
            Location  = new Point(200, 610),
            ForeColor = AppTheme.MutedText
        };

        Controls.AddRange(new Control[]
        {
            _lblTitle, _btnNew, _btnRefresh,
            _grid, _btnOpen, _lblStatus
        });
    }

    private void LoadWarehouses()
    {
        try { _warehouses = _repo.GetAll(); }
        catch { /* silent */ }
    }

    private void LoadTransfers()
    {
        try
        {
            _transfers = _repo.GetTransfers();
            _grid.Rows.Clear();
            foreach (var t in _transfers)
            {
                var row = _grid.Rows.Add(t.TransferNumber, t.FromWarehouse, t.ToWarehouse,
                    t.TransferDate.ToString("yyyy-MM-dd"), t.StatusAr, t.CreatedByName);
                _grid.Rows[row].Tag = t;
                if (t.Status == "approved")
                    _grid.Rows[row].DefaultCellStyle.ForeColor = AppTheme.Success;
            }
            _lblStatus.Text = $"إجمالي: {_transfers.Count}  |  معتمد: {_transfers.Count(x => x.Status == "approved")}  |  مسودة: {_transfers.Count(x => x.Status == "draft")}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل التحويلات:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnNew(object? sender, EventArgs e)
    {
        if (_warehouses.Count < 2)
        {
            MessageBox.Show("يجب وجود مستودعين على الأقل لإجراء التحويل.",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new NewTransferSetupDialog(_warehouses);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var id = _repo.CreateTransfer(dlg.FromWarehouseId, dlg.ToWarehouseId,
                                           dlg.Notes, SessionContext.CurrentUser!.Id);
            var editDlg = new TransferDialog(id, false);
            editDlg.ShowDialog(this);
            LoadTransfers();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في إنشاء التحويل:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenTransfer()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var t  = (WarehouseTransfer)_grid.SelectedRows[0].Tag!;
        var ro = t.Status == "approved";
        var dlg = new TransferDialog(t.Id, ro);
        dlg.ShowDialog(this);
        LoadTransfers();
    }
}

// ── نموذج اختيار المستودعين لتحويل جديد ─────────────────────
internal class NewTransferSetupDialog : Form
{
    public int    FromWarehouseId { get; private set; }
    public int    ToWarehouseId   { get; private set; }
    public string Notes           { get; private set; } = "";

    private ComboBox _cmbFrom = null!;
    private ComboBox _cmbTo   = null!;
    private TextBox  _txtNotes= null!;
    private readonly List<Warehouse> _warehouses;

    public NewTransferSetupDialog(List<Warehouse> warehouses)
    {
        _warehouses = warehouses;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text              = "تحويل جديد";
        Size              = new Size(400, 280);
        StartPosition     = FormStartPosition.CenterParent;
        MinimizeBox       = false; MaximizeBox = false;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;
        FormBorderStyle   = FormBorderStyle.FixedDialog;

        var lblFrom = new Label { Text = "من مستودع:", AutoSize = true, Location = new Point(20, 24), ForeColor = AppTheme.DarkText };
        _cmbFrom = new ComboBox { Location = new Point(120, 20), Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppTheme.Surface };
        foreach (var w in _warehouses) _cmbFrom.Items.Add(w.Name);
        _cmbFrom.SelectedIndex = 0;

        var lblTo = new Label { Text = "إلى مستودع:", AutoSize = true, Location = new Point(20, 64), ForeColor = AppTheme.DarkText };
        _cmbTo = new ComboBox { Location = new Point(120, 60), Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList, BackColor = AppTheme.Surface };
        foreach (var w in _warehouses) _cmbTo.Items.Add(w.Name);
        _cmbTo.SelectedIndex = _warehouses.Count > 1 ? 1 : 0;

        var lblNotes = new Label { Text = "ملاحظات:", AutoSize = true, Location = new Point(20, 104), ForeColor = AppTheme.DarkText };
        _txtNotes = new TextBox { Location = new Point(120, 100), Width = 220,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };

        var btnOk = new Button
        {
            Text = "إنشاء", Location = new Point(120, 150), Size = new Size(100, 34),
            BackColor = AppTheme.Primary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) =>
        {
            if (_cmbFrom.SelectedIndex == _cmbTo.SelectedIndex)
            {
                MessageBox.Show("المستودع المصدر والوجهة يجب أن يكونا مختلفين.",
                    "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            FromWarehouseId = _warehouses[_cmbFrom.SelectedIndex].Id;
            ToWarehouseId   = _warehouses[_cmbTo.SelectedIndex].Id;
            Notes           = _txtNotes.Text.Trim();
            DialogResult    = DialogResult.OK;
            Close();
        };

        var btnCancel = new Button
        {
            Text = "إلغاء", Location = new Point(235, 150), Size = new Size(100, 34),
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText, FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange(new Control[]
            { lblFrom, _cmbFrom, lblTo, _cmbTo, lblNotes, _txtNotes, btnOk, btnCancel });
    }
}
