using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-017 — شاشة قائمة مرتجعات المبيعات</summary>
internal sealed class SalesReturnsView : UserControl
{
    private readonly DataGridView _grid;
    private readonly DateTimePicker _fromPicker;
    private readonly DateTimePicker _toPicker;
    private readonly Button         _filterBtn;
    private readonly Button         _newBtn;
    private readonly Label          _totalLabel;
    private readonly Label          _feedbackLabel;
    private readonly Label          _countLabel;
    private List<SalesReturn>       _all = new();

    public SalesReturnsView()
    {
        _grid       = new DataGridView();
        _fromPicker = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        _toPicker   = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        _filterBtn  = new Button { Text = "🔍 تصفية" };
        _newBtn     = new Button { Text = "➕ مرتجع جديد" };
        _totalLabel    = new Label();
        _feedbackLabel = new Label();
        _countLabel    = new Label();

        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        BuildUI();
        WireEvents();
        LoadData();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(8), BackColor = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildGrid(),    0, 1);
        root.Controls.Add(BuildFooter(),  0, 2);
        Controls.Add(root);
    }

    private Control BuildToolbar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, BackColor = AppTheme.Surface
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // تاريخ
        var datePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        var lblFrom = new Label { Text = "من:", AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.DarkText, TextAlign = ContentAlignment.MiddleLeft };
        var lblTo   = new Label { Text = "إلى:", AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.DarkText, TextAlign = ContentAlignment.MiddleLeft };
        _fromPicker.Font = _toPicker.Font = AppTheme.BodyFont;
        _fromPicker.Width = _toPicker.Width = 110;
        AppTheme.StyleSecondaryButton(_filterBtn);
        _filterBtn.Width = 80;

        datePanel.Controls.Add(_filterBtn);
        datePanel.Controls.Add(lblTo);
        datePanel.Controls.Add(_toPicker);
        datePanel.Controls.Add(lblFrom);
        datePanel.Controls.Add(_fromPicker);

        // buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        AppTheme.StylePrimaryButton(_newBtn);
        _newBtn.Width = 160; _newBtn.Margin = new Padding(4, 0, 4, 0);
        btnPanel.Controls.Add(_newBtn);

        bar.Controls.Add(btnPanel,   0, 0);
        bar.Controls.Add(datePanel,  1, 0);
        return bar;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _grid.Dock                  = DockStyle.Fill;
        _grid.ReadOnly              = true;
        _grid.AllowUserToAddRows    = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect           = false;
        _grid.AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible     = false;
        _grid.BorderStyle           = BorderStyle.None;
        _grid.Font                  = AppTheme.BodyFont;
        _grid.BackgroundColor       = AppTheme.Surface;
        _grid.GridColor             = AppTheme.Border;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.Primary;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _grid.EnableHeadersVisualStyles               = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId",      HeaderText = "#",             Width = 50, FillWeight = 4 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRetNum",  HeaderText = "رقم المرتجع",   FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "التاريخ",        FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colInvNum",  HeaderText = "الفاتورة الأصلية", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCust",    HeaderText = "العميل",         FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCashier", HeaderText = "الكاشير",        FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",   HeaderText = "الإجمالي",       FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRefund",  HeaderText = "طريقة الرد",     FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "الحالة",         FillWeight = 8  });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildFooter()
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, BackColor = AppTheme.Surface
        };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text      = "اضغط «مرتجع جديد» لإنشاء مرتجع.";

        _totalLabel.Font      = new Font("Tahoma", 9.5F, FontStyle.Bold);
        _totalLabel.ForeColor = AppTheme.Primary;
        _totalLabel.TextAlign = ContentAlignment.MiddleLeft;
        _totalLabel.AutoSize  = true;
        _totalLabel.Margin    = new Padding(12, 0, 0, 0);

        _countLabel.Font      = AppTheme.SmallFont;
        _countLabel.ForeColor = AppTheme.MutedText;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft;
        _countLabel.AutoSize  = true;
        _countLabel.Margin    = new Padding(12, 0, 0, 0);

        p.Controls.Add(_feedbackLabel, 0, 0);
        p.Controls.Add(_totalLabel,    1, 0);
        p.Controls.Add(_countLabel,    2, 0);
        return p;
    }

    private void WireEvents()
    {
        _filterBtn.Click += (_, _) => LoadData();
        _newBtn.Click    += (_, _) => OpenNewReturn();
    }

    private void LoadData()
    {
        try
        {
            _all = new SalesReturnRepository().GetAll(_fromPicker.Value, _toPicker.Value);
            PopulateGrid(_all);
            ShowInfo($"تم تحميل {_all.Count} مرتجع.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid(List<SalesReturn> items)
    {
        _grid.Rows.Clear();
        decimal grandTotal = 0;
        foreach (var r in items)
        {
            _grid.Rows.Add(
                r.Id, r.ReturnNumber,
                r.ReturnDate.ToString("dd/MM/yyyy HH:mm"),
                r.OriginalInvoiceNum,
                r.CustomerName, r.CashierName,
                r.TotalAmount.ToString("N2"),
                r.RefundMethodAr,
                r.Status == "completed" ? "✅ مكتمل" : r.Status
            );
            grandTotal += r.TotalAmount;
        }
        _countLabel.Text = $"الإجمالي: {items.Count}";
        _totalLabel.Text = $"مجموع المرتجعات: {grandTotal:N2} ج";
    }

    private void OpenNewReturn()
    {
        using var dlg = new SalesReturnDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            LoadData();
            ShowSuccess($"تم حفظ المرتجع {dlg.CreatedReturn?.ReturnNumber} بنجاح.");
        }
    }

    private void ShowSuccess(string m) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = m; }
    private void ShowError(string m)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
    private void ShowInfo(string m)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = m; }
}
