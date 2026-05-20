using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-012 — شاشة قائمة فواتير الشراء مع أزرار إنشاء / عرض / اعتماد
/// </summary>
internal sealed class PurchaseInvoicesView : UserControl
{
    private readonly DataGridView   _grid;
    private readonly DateTimePicker _fromPicker;
    private readonly DateTimePicker _toPicker;
    private readonly ComboBox       _statusCombo;
    private readonly TextBox        _supplierBox;
    private readonly Button         _searchBtn;
    private readonly Button         _newBtn;
    private readonly Button         _openBtn;
    private readonly Label          _feedbackLbl;
    private readonly Label          _countLbl;
    private List<PurchaseInvoice>   _all = new();

    public PurchaseInvoicesView()
    {
        _grid        = new DataGridView();
        _fromPicker  = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
        _toPicker    = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        _statusCombo = AppTheme.CreateComboBox();
        _supplierBox = AppTheme.CreateTextBox("تصفية بالمورد...");
        _searchBtn   = new Button { Text = "🔍 بحث" };
        _newBtn      = new Button { Text = "➕ فاتورة جديدة" };
        _openBtn     = new Button { Text = "📂 فتح / تعديل" };
        _feedbackLbl = new Label();
        _countLbl    = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        _statusCombo.Items.AddRange(new object[] { "الكل", "مسودة", "معتمدة", "ملغية" });
        _statusCombo.SelectedIndex = 0;

        BuildUI();
        WireEvents();
        LoadData();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 136F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(),    0, 1);
        root.Controls.Add(BuildFooter(),  0, 2);
        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var wrap = AppTheme.CreateCard();
        wrap.Dock    = DockStyle.Fill;
        wrap.Padding = new Padding(16, 10, 16, 10);

        // ── صف 1: العنوان ──────────────────────────────────
        var titleLbl = new Label
        {
            Text      = "إدارة فواتير المشتريات",
            Dock      = DockStyle.Top,
            Height    = 34,
            Font      = new Font("Tahoma", 14F, FontStyle.Bold),
            ForeColor = AppTheme.Primary,
            TextAlign = ContentAlignment.MiddleRight,
            Padding   = new Padding(0, 2, 0, 0)
        };

        // ── صف 2: الوصف ────────────────────────────────────
        var subLbl = new Label
        {
            Text      = "فلترة سريعة، عرض واضح، والوصول المباشر لإنشاء أو فتح الفاتورة.",
            Dock      = DockStyle.Top,
            Height    = 22,
            Font      = new Font("Tahoma", 9F),
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleRight
        };

        // ── صف 3: شريط الأدوات ────────────────────────────
        var bar = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 40,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            AutoSize      = false,
            Padding       = new Padding(0, 4, 0, 0)
        };

        AppTheme.StylePrimaryButton(_newBtn);
        _newBtn.Width  = 150; _newBtn.Height = 32; _newBtn.Margin = new Padding(4, 0, 0, 0);

        AppTheme.StyleSecondaryButton(_openBtn);
        _openBtn.Width = 130; _openBtn.Height = 32; _openBtn.Margin = new Padding(4, 0, 0, 0);

        AppTheme.StyleSecondaryButton(_searchBtn);
        _searchBtn.Width = 80; _searchBtn.Height = 32; _searchBtn.Margin = new Padding(4, 0, 0, 0);

        var fromLbl  = AppTheme.CreateFieldLabel("من:");
        fromLbl.Margin = new Padding(8, 8, 2, 0);
        _fromPicker.Width  = 108; _fromPicker.Height = 28; _fromPicker.Margin = new Padding(2, 2, 0, 0);

        var toLbl = AppTheme.CreateFieldLabel("إلى:");
        toLbl.Margin = new Padding(8, 8, 2, 0);
        _toPicker.Width    = 108; _toPicker.Height   = 28; _toPicker.Margin   = new Padding(2, 2, 0, 0);

        var statLbl = AppTheme.CreateFieldLabel("الحالة:");
        statLbl.Margin = new Padding(8, 8, 2, 0);
        _statusCombo.Width = 95; _statusCombo.Margin = new Padding(2, 2, 0, 0);

        _supplierBox.Width = 150; _supplierBox.Margin = new Padding(2, 2, 0, 0);

        bar.Controls.Add(_newBtn);
        bar.Controls.Add(_openBtn);
        bar.Controls.Add(_searchBtn);
        bar.Controls.Add(statLbl);    bar.Controls.Add(_statusCombo);
        bar.Controls.Add(toLbl);      bar.Controls.Add(_toPicker);
        bar.Controls.Add(fromLbl);    bar.Controls.Add(_fromPicker);
        bar.Controls.Add(_supplierBox);

        // ترتيب الإضافة معكوس (Bottom to Top) مع Dock=Top
        wrap.Controls.Add(bar);
        wrap.Controls.Add(subLbl);
        wrap.Controls.Add(titleLbl);
        return wrap;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false; _grid.BorderStyle = BorderStyle.None;
        _grid.Font = AppTheme.BodyFont; _grid.BackgroundColor = AppTheme.Surface; _grid.GridColor = AppTheme.Border;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.EnableHeadersVisualStyles = false;
        _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.RowTemplate.Height = 36;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",    HeaderText = "#",           Width = 50, FillWeight = 4 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNum",   HeaderText = "رقم الفاتورة",FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",  HeaderText = "التاريخ",     FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSup",   HeaderText = "المورد",      FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWh",    HeaderText = "المستودع",    FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPay",   HeaderText = "الدفع",       FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNet",   HeaderText = "الصافي",      FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPaid",  HeaderText = "المدفوع",     FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRem",   HeaderText = "المتبقي",     FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",HeaderText = "الحالة",      FillWeight = 8  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.Surface };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _feedbackLbl.Dock = DockStyle.Fill; _feedbackLbl.Font = new Font("Tahoma", 8.5F);
        _feedbackLbl.ForeColor = AppTheme.MutedText; _feedbackLbl.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLbl.Text = "انقر نقراً مزدوجاً لفتح فاتورة — الفواتير المعتمدة لا يمكن تعديلها.";
        _countLbl.Font = new Font("Tahoma", 8.5F); _countLbl.ForeColor = AppTheme.MutedText;
        _countLbl.TextAlign = ContentAlignment.MiddleLeft; _countLbl.AutoSize = true;
        p.Controls.Add(_feedbackLbl, 0, 0); p.Controls.Add(_countLbl, 1, 0);
        return p;
    }

    private void WireEvents()
    {
        _newBtn.Click              += (_, _) => OpenInvoice(null);
        _openBtn.Click             += (_, _) => OpenSelected();
        _searchBtn.Click           += (_, _) => LoadData();
        _grid.CellDoubleClick      += (_, _) => OpenSelected();
    }

    private void LoadData()
    {
        try
        {
            string? statusFilter = _statusCombo.SelectedIndex switch
            {
                1 => "draft", 2 => "approved", 3 => "cancelled", _ => null
            };
            var repo = new PurchaseRepository();
            _all = repo.GetAll(
                from: _fromPicker.Value.Date,
                to:   _toPicker.Value.Date,
                status: statusFilter);

            // فلتر المورد نصياً
            var q = _supplierBox.Text.Trim();
            if (!string.IsNullOrEmpty(q))
                _all = _all.Where(i => i.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            PopulateGrid();
            ShowInfo($"تم تحميل {_all.Count} فاتورة.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();
        foreach (var inv in _all)
        {
            int idx = _grid.Rows.Add(
                inv.Id,
                inv.InvoiceNumber,
                inv.InvoiceDate.ToString("dd/MM/yyyy"),
                inv.SupplierName,
                inv.WarehouseName,
                inv.PaymentAr,
                inv.NetTotal.ToString("N2"),
                inv.PaidAmount.ToString("N2"),
                inv.Remaining > 0 ? inv.Remaining.ToString("N2") : "صفر",
                inv.StatusAr
            );
            _grid.Rows[idx].DefaultCellStyle.ForeColor = inv.Status switch
            {
                "approved"  => AppTheme.Success,
                "cancelled" => AppTheme.MutedText,
                _           => AppTheme.Warning
            };
        }
        _countLbl.Text = $"الإجمالي: {_all.Count}";
    }

    private void OpenSelected()
    {
        if (_grid.SelectedRows.Count == 0) return;
        int id = (int)_grid.SelectedRows[0].Cells["colId"].Value;
        var inv = _all.FirstOrDefault(i => i.Id == id);
        if (inv is null) return;

        try
        {
            var full = new PurchaseRepository().GetById(inv.Id);
            OpenInvoice(full);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void OpenInvoice(PurchaseInvoice? inv)
    {
        using var dlg = new PurchaseInvoiceDialog(inv);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess("تم تحديث قائمة الفواتير.");
        }
    }

    private void ShowSuccess(string m) { _feedbackLbl.ForeColor = AppTheme.Success;   _feedbackLbl.Text = m; }
    private void ShowError(string m)   { _feedbackLbl.ForeColor = AppTheme.Danger;    _feedbackLbl.Text = m; }
    private void ShowInfo(string m)    { _feedbackLbl.ForeColor = AppTheme.MutedText; _feedbackLbl.Text = m; }
}
