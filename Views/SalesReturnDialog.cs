using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-017 — نموذج إنشاء مرتجع مبيعات</summary>
internal sealed class SalesReturnDialog : Form
{
    private readonly TextBox         _invoiceNumBox;
    private readonly Button          _searchBtn;
    private readonly Label           _invoiceInfoLabel;
    private readonly DataGridView    _grid;
    private readonly RadioButton     _rbCash;
    private readonly RadioButton     _rbCredit;
    private readonly TextBox         _notesBox;
    private readonly Label           _totalLabel;
    private readonly Button          _saveBtn;
    private readonly Button          _cancelBtn;
    private readonly Label           _feedbackLabel;

    private SalesInvoice? _invoice;
    private int           _warehouseId;

    public SalesReturn? CreatedReturn { get; private set; }

    public SalesReturnDialog()
    {
        Text            = "مرتجع مبيعات جديد";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(860, 620);
        MinimumSize     = new Size(760, 560);
        BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        _invoiceNumBox    = AppTheme.CreateTextBox("رقم الفاتورة (مثال: SAL-00001)");
        _searchBtn        = new Button { Text = "🔍 بحث" };
        _invoiceInfoLabel = new Label();
        _grid             = new DataGridView();
        _rbCash           = new RadioButton { Text = "نقدي", Checked = true, AutoSize = true, Font = AppTheme.BodyFont };
        _rbCredit         = new RadioButton { Text = "رصيد دائن", AutoSize = true, Font = AppTheme.BodyFont };
        _notesBox         = AppTheme.CreateTextBox("ملاحظات اختيارية...");
        _totalLabel       = new Label();
        _saveBtn          = new Button { Text = "✅ حفظ المرتجع" };
        _cancelBtn        = new Button { Text = "❌ إلغاء" };
        _feedbackLabel    = new Label();

        // جلب المستودع الافتراضي
        var wh = new SalesRepository().GetDefaultWarehouse();
        _warehouseId = wh?.Id ?? 1;

        BuildUI();
        WireEvents();
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(16), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));  // بحث
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));  // معلومات الفاتورة
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // الجريد
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // الإجمالي + طريقة الرد
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // أزرار

        root.Controls.Add(BuildSearchRow(),   0, 0);
        root.Controls.Add(_invoiceInfoLabel,  0, 1);
        root.Controls.Add(BuildGrid(),        0, 2);
        root.Controls.Add(BuildBottomBar(),   0, 3);
        root.Controls.Add(BuildButtons(),     0, 4);
        Controls.Add(root);

        // تهيئة الـ labels
        _invoiceInfoLabel.Dock      = DockStyle.Fill;
        _invoiceInfoLabel.Font      = AppTheme.SmallFont;
        _invoiceInfoLabel.ForeColor = AppTheme.MutedText;
        _invoiceInfoLabel.TextAlign = ContentAlignment.MiddleRight;
        _invoiceInfoLabel.Text      = "أدخل رقم الفاتورة ثم اضغط بحث لاسترجاع أصنافها.";

        _totalLabel.Font      = new Font("Tahoma", 13F, FontStyle.Bold);
        _totalLabel.ForeColor = AppTheme.Primary;
        _totalLabel.TextAlign = ContentAlignment.MiddleRight;
        _totalLabel.Text      = "الإجمالي: 0.00 ج";

        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
    }

    private Control BuildSearchRow()
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, BackColor = AppTheme.Background
        };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));

        _invoiceNumBox.Dock = DockStyle.Fill;
        _invoiceNumBox.Font = AppTheme.BodyFont;
        AppTheme.StylePrimaryButton(_searchBtn);
        _searchBtn.Dock = DockStyle.Fill;
        _searchBtn.Margin = new Padding(6, 0, 0, 0);

        p.Controls.Add(_invoiceNumBox, 0, 0);
        p.Controls.Add(_searchBtn,     1, 0);
        return p;
    }

    private Control BuildGrid()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(4);

        _grid.Dock                  = DockStyle.Fill;
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

        // أعمدة الجريد
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colSel", HeaderText = "✔", Width = 40, FillWeight = 4
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode",     HeaderText = "الكود",       FillWeight = 12, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",     HeaderText = "الصنف",       FillWeight = 30, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",     HeaderText = "الوحدة",      FillWeight = 10, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSoldQty",  HeaderText = "المباع",      FillWeight = 10, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReturnQty",HeaderText = "الكمية المرتجعة", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",    HeaderText = "السعر",       FillWeight = 10, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "الإجمالي",    FillWeight = 10, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemId",   Visible    = false });

        card.Controls.Add(_grid);
        return card;
    }

    private Control BuildBottomBar()
    {
        var p = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, BackColor = AppTheme.Background,
            Padding = new Padding(0, 6, 0, 0)
        };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));

        // طريقة الرد
        var refundPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = AppTheme.Background
        };
        var refundLabel = new Label { Text = "طريقة الرد:", AutoSize = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.DarkText };
        refundPanel.Controls.Add(_rbCash);
        refundPanel.Controls.Add(_rbCredit);
        refundPanel.Controls.Add(refundLabel);

        // ملاحظات
        _notesBox.Width = 200;

        _totalLabel.Dock = DockStyle.Fill;
        _feedbackLabel.Dock = DockStyle.Fill;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        p.Controls.Add(refundPanel,   0, 0);
        p.Controls.Add(_notesBox,     1, 0);
        p.Controls.Add(_totalLabel,   2, 0);
        return p;
    }

    private Control BuildButtons()
    {
        var p = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 6, 0, 0),
            BackColor = AppTheme.Background
        };
        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(_cancelBtn);
        _saveBtn.Width = 180; _cancelBtn.Width = 100;
        _saveBtn.Margin = _cancelBtn.Margin = new Padding(6, 0, 0, 0);

        _feedbackLabel.AutoSize  = true;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleLeft;

        p.Controls.Add(_saveBtn);
        p.Controls.Add(_cancelBtn);
        p.Controls.Add(_feedbackLabel);
        return p;
    }

    private void WireEvents()
    {
        _searchBtn.Click     += (_, _) => SearchInvoice();
        _invoiceNumBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) SearchInvoice(); };
        _grid.CellValueChanged += (_, _) => RecalcTotal();
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _saveBtn.Click   += (_, _) => SaveReturn();
        _cancelBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;
    }

    private void SearchInvoice()
    {
        var num = _invoiceNumBox.Text.Trim();
        if (string.IsNullOrEmpty(num)) { ShowError("أدخل رقم الفاتورة."); return; }

        try
        {
            _invoice = new SalesReturnRepository().GetInvoiceByNumber(num);
            if (_invoice is null)
            {
                ShowError("لم يتم العثور على الفاتورة أو تم إلغاؤها.");
                return;
            }
            if (!_invoice.Lines.Any())
            {
                ShowError("جميع أصناف هذه الفاتورة تم إرجاعها مسبقاً.");
                return;
            }

            _warehouseId = _invoice.WarehouseId;
            _invoiceInfoLabel.Text = $"فاتورة: {_invoice.InvoiceNumber} | التاريخ: {_invoice.InvoiceDate:dd/MM/yyyy} | العميل: {_invoice.CustomerName} | الإجمالي: {_invoice.NetTotal:N2} ج";
            _invoiceInfoLabel.ForeColor = AppTheme.Primary;

            PopulateGrid();
            ShowInfo("اختر الأصناف المطلوب إرجاعها وحدد الكميات.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void PopulateGrid()
    {
        _grid.Rows.Clear();
        if (_invoice is null) return;
        foreach (var l in _invoice.Lines)
        {
            int idx = _grid.Rows.Add(
                false,               // colSel
                l.ItemCode,          // colCode
                l.ItemName,          // colName
                l.UnitName,          // colUnit
                l.Quantity,          // colSoldQty (المتاح للإرجاع)
                l.Quantity,          // colReturnQty (افتراضي = الكل)
                l.UnitPrice.ToString("N2"),
                (l.Quantity * l.UnitPrice).ToString("N2"),
                l.ItemId             // colItemId
            );
            // تلوين
            _grid.Rows[idx].DefaultCellStyle.BackColor = AppTheme.Surface;
        }
        RecalcTotal();
    }

    private void RecalcTotal()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            bool   selected  = row.Cells["colSel"].Value is true;
            if (!selected) { row.Cells["colTotal"].Value = "0.00"; continue; }

            if (!decimal.TryParse(row.Cells["colReturnQty"].Value?.ToString(), out decimal retQty) || retQty <= 0)
            { row.Cells["colTotal"].Value = "0.00"; continue; }

            if (!decimal.TryParse(row.Cells["colSoldQty"].Value?.ToString(), out decimal soldQty))
                soldQty = retQty;

            // تحقق: لا يمكن إرجاع أكثر من المباع
            if (retQty > soldQty)
            {
                retQty = soldQty;
                row.Cells["colReturnQty"].Value = retQty;
            }

            if (!decimal.TryParse(row.Cells["colPrice"].Value?.ToString(), out decimal price))
                price = 0;

            decimal lineTotal = retQty * price;
            row.Cells["colTotal"].Value = lineTotal.ToString("N2");
            total += lineTotal;
        }
        _totalLabel.Text = $"الإجمالي: {total:N2} ج";
    }

    private void SaveReturn()
    {
        if (_invoice is null) { ShowError("ابحث عن فاتورة أولاً."); return; }

        var lines = new List<SalesReturnLine>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells["colSel"].Value is not true) continue;
            if (!decimal.TryParse(row.Cells["colReturnQty"].Value?.ToString(), out decimal qty) || qty <= 0) continue;
            if (!decimal.TryParse(row.Cells["colSoldQty"].Value?.ToString(),   out decimal sold)) continue;
            if (qty > sold) { ShowError($"كمية الإرجاع لصنف [{row.Cells["colName"].Value}] أكبر من الكمية المباعة."); return; }
            if (!decimal.TryParse(row.Cells["colPrice"].Value?.ToString(), out decimal price)) continue;

            lines.Add(new SalesReturnLine
            {
                ItemId    = (int)row.Cells["colItemId"].Value,
                ItemCode  = row.Cells["colCode"].Value?.ToString() ?? "",
                ItemName  = row.Cells["colName"].Value?.ToString() ?? "",
                UnitName  = row.Cells["colUnit"].Value?.ToString() ?? "",
                SoldQty   = sold,
                ReturnQty = qty,
                UnitPrice = price
            });
        }

        if (!lines.Any()) { ShowError("اختر صنفاً واحداً على الأقل للإرجاع."); return; }

        decimal total = lines.Sum(l => l.LineTotal);

        var ret = new SalesReturn
        {
            ReturnNumber      = new SalesReturnRepository().NextReturnNumber(),
            OriginalInvoiceId = _invoice.Id,
            OriginalInvoiceNum= _invoice.InvoiceNumber,
            CustomerName      = _invoice.CustomerName,
            CashierId         = SessionContext.CurrentUser!.Id,
            WarehouseId       = _warehouseId,
            ReturnDate        = DateTime.Now,
            TotalAmount       = total,
            RefundMethod      = _rbCash.Checked ? "cash" : "credit_note",
            Status            = "completed",
            Notes             = _notesBox.Text.Trim(),
            Lines             = lines
        };

        try
        {
            int newId = new SalesReturnRepository().SaveReturn(ret);
            ret.Id = newId;
            CreatedReturn = ret;
            AuditService.LogCreate("public.sales_returns", newId);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ShowError(string m) { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
    private void ShowInfo(string m)  { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = m; }
}
