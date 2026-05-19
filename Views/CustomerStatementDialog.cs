using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>كشف حساب العميل — TASK-015</summary>
internal sealed class CustomerStatementDialog : Form
{
    private readonly Customer       _customer;
    private readonly DataGridView   _grid;
    private readonly DateTimePicker _fromPicker;
    private readonly DateTimePicker _toPicker;
    private readonly Label          _totalLabel;
    private readonly Label          _paidLabel;
    private readonly Label          _remainingLabel;
    private readonly Label          _pointsLabel;
    private readonly Label          _feedbackLabel;

    public CustomerStatementDialog(Customer customer)
    {
        _customer = customer;
        _grid           = new DataGridView();
        _fromPicker     = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3) };
        _toPicker       = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        _totalLabel     = new Label();
        _paidLabel      = new Label();
        _remainingLabel = new Label();
        _pointsLabel    = new Label();
        _feedbackLabel  = new Label();

        Text              = $"كشف حساب — {customer.Name}";
        Size              = new Size(920, 600);
        MinimumSize       = new Size(860, 560);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle   = FormBorderStyle.Sizable;

        BuildUI();
        LoadStatement();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));

        root.Controls.Add(BuildInfoBar(),   0, 0);
        root.Controls.Add(BuildFilterBar(), 0, 1);
        root.Controls.Add(BuildGrid(),      0, 2);
        root.Controls.Add(BuildTotalsBar(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildInfoBar()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 6, 12, 6);

        var info = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = AppTheme.SectionFont,
            ForeColor = AppTheme.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };

        var parts = new List<string> { _customer.Name };
        if (!string.IsNullOrEmpty(_customer.Phone)) parts.Add($"📞 {_customer.Phone}");
        if (_customer.CreditLimit > 0)             parts.Add($"حد الائتمان: {_customer.CreditLimit:N2}");
        parts.Add($"🎯 نقاط الولاء: {_customer.LoyaltyPoints:N0}");
        info.Text = string.Join("   |   ", parts);

        card.Controls.Add(info);
        return card;
    }

    private Control BuildFilterBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 5, BackColor = AppTheme.Background
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        _fromPicker.Dock = DockStyle.Fill;
        _toPicker.Dock   = DockStyle.Fill;

        var searchBtn = new Button { Text = "🔍 عرض", Dock = DockStyle.Left, Width = 100 };
        AppTheme.StylePrimaryButton(searchBtn);
        searchBtn.Click += (_, _) => LoadStatement();

        bar.Controls.Add(AppTheme.CreateFieldLabel("من:"),  0, 0);
        bar.Controls.Add(_fromPicker,                        1, 0);
        bar.Controls.Add(AppTheme.CreateFieldLabel("إلى:"), 2, 0);
        bar.Controls.Add(_toPicker,                          3, 0);
        bar.Controls.Add(searchBtn,                          4, 0);
        return bar;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(4);

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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNum",     HeaderText = "رقم الفاتورة",    FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",    HeaderText = "التاريخ",         FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",   HeaderText = "إجمالي الفاتورة", FillWeight = 13 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPaid",    HeaderText = "المدفوع",         FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRemain",  HeaderText = "المتبقي",         FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPayment", HeaderText = "طريقة الدفع",    FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",  HeaderText = "الحالة",          FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes",   HeaderText = "ملاحظات",         FillWeight = 19 });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildTotalsBar()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 8, 12, 8);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 8
        };
        for (int i = 0; i < 8; i++)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));

        void AddTotal(Label lbl, string caption, Color color, int col)
        {
            lbl.Dock      = DockStyle.Fill;
            lbl.Font      = new Font("Tahoma", 10F, FontStyle.Bold);
            lbl.ForeColor = color;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            var cap = new Label
            {
                Text = caption, Dock = DockStyle.Fill,
                Font = AppTheme.SmallFont, ForeColor = AppTheme.MutedText,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var cell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            cell.Controls.Add(cap, 0, 0);
            cell.Controls.Add(lbl, 0, 1);
            layout.Controls.Add(cell, col, 0);
        }

        AddTotal(_totalLabel,     "إجمالي الفواتير",  AppTheme.DarkText, 0);
        AddTotal(_paidLabel,      "إجمالي المدفوع",   AppTheme.Success,  2);
        AddTotal(_remainingLabel, "إجمالي المستحق",   AppTheme.Danger,   4);
        AddTotal(_pointsLabel,    "🎯 نقاط الولاء",    AppTheme.Primary,  6);

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        layout.Controls.Add(_feedbackLabel, 7, 0);

        card.Controls.Add(layout);
        return card;
    }

    private void LoadStatement()
    {
        try
        {
            var repo = new CustomerRepository();
            var stmt = repo.GetStatement(_customer.Id, _fromPicker.Value.Date, _toPicker.Value.Date);
            var fresh = repo.GetById(_customer.Id);

            _grid.Rows.Clear();
            foreach (var row in stmt)
            {
                int idx = _grid.Rows.Add(
                    row.InvoiceNumber,
                    row.InvoiceDate.ToString("dd/MM/yyyy"),
                    row.NetTotal.ToString("N2"),
                    row.PaidAmount.ToString("N2"),
                    row.Remaining > 0 ? row.Remaining.ToString("N2") : "—",
                    row.PaymentAr,
                    row.StatusAr,
                    row.Notes
                );
                if (row.Remaining > 0)
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.Warning;
                if (row.Status == "cancelled")
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = AppTheme.MutedText;
            }

            decimal total     = stmt.Sum(x => x.NetTotal);
            decimal paid      = stmt.Sum(x => x.PaidAmount);
            decimal remaining = stmt.Sum(x => x.Remaining);

            _totalLabel.Text     = total.ToString("N2") + " ج";
            _paidLabel.Text      = paid.ToString("N2")  + " ج";
            _remainingLabel.Text = remaining.ToString("N2") + " ج";
            _pointsLabel.Text    = (fresh?.LoyaltyPoints ?? _customer.LoyaltyPoints).ToString("N0") + " نقطة";

            _feedbackLabel.Text      = $"{stmt.Count} فاتورة في الفترة المحددة.";
            _feedbackLabel.ForeColor = AppTheme.MutedText;
        }
        catch (Exception ex)
        {
            _feedbackLabel.ForeColor = AppTheme.Danger;
            _feedbackLabel.Text      = $"خطأ: {ex.Message}";
        }
    }
}
