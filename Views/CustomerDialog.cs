using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-015 — نموذج إضافة / تعديل عميل</summary>
internal sealed class CustomerDialog : Form
{
    private readonly TextBox  _codeBox;
    private readonly TextBox  _nameBox;
    private readonly TextBox  _phoneBox;
    private readonly TextBox  _emailBox;
    private readonly TextBox  _addressBox;
    private readonly TextBox  _creditBox;
    private readonly Label    _pointsLbl;
    private readonly CheckBox _activeChk;
    private readonly Button   _saveBtn;
    private readonly Label    _feedbackLbl;

    private readonly Customer _customer;
    private readonly bool     _isNew;

    public CustomerDialog(Customer? existing = null)
    {
        _codeBox     = AppTheme.CreateTextBox();
        _nameBox     = AppTheme.CreateTextBox();
        _phoneBox    = AppTheme.CreateTextBox();
        _emailBox    = AppTheme.CreateTextBox();
        _addressBox  = AppTheme.CreateTextBox();
        _creditBox   = AppTheme.CreateTextBox();
        _pointsLbl   = new Label();
        _activeChk   = new CheckBox { Text = "نشط", Checked = true };
        _saveBtn     = new Button   { Text = "💾 حفظ" };
        _feedbackLbl = new Label();

        _isNew    = existing is null;
        _customer = existing ?? new Customer();

        Text              = _isNew ? "عميل جديد" : $"تعديل: {_customer.Name}";
        Size              = new Size(520, 480);
        MinimumSize       = new Size(480, 440);
        StartPosition     = FormStartPosition.CenterParent;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;

        BuildUI();
        LoadData();
        _saveBtn.Click += (_, _) => Save();
        _nameBox.TextChanged += (_, _) => _feedbackLbl.Text = "";
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));  // عنوان
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // الحقول
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));  // الأزرار

        // عنوان
        var titleLbl = new Label
        {
            Text      = _isNew ? "➕ إضافة عميل جديد" : "✏️ تعديل بيانات العميل",
            Dock      = DockStyle.Fill,
            Font      = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary,
            TextAlign = ContentAlignment.MiddleRight
        };
        root.Controls.Add(titleLbl, 0, 0);

        // الحقول
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill; card.Padding = new Padding(12);

        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        for (int i = 0; i < 7; i++) tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        void AddField(Control lbl, Control ctrl, int col, int row)
        {
            lbl.Dock = DockStyle.Top; ctrl.Dock = DockStyle.Fill;
            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 0, 4, 0) };
            wrap.Controls.Add(ctrl); wrap.Controls.Add(lbl);
            tbl.Controls.Add(wrap, col, row);
        }

        AddField(AppTheme.CreateFieldLabel("كود العميل:"),   _codeBox,    0, 0);
        AddField(AppTheme.CreateFieldLabel("الاسم: *"),      _nameBox,    1, 0);
        AddField(AppTheme.CreateFieldLabel("الهاتف:"),       _phoneBox,   0, 1);
        AddField(AppTheme.CreateFieldLabel("البريد الإلكتروني:"), _emailBox, 1, 1);
        AddField(AppTheme.CreateFieldLabel("العنوان:"),      _addressBox, 0, 2);
        AddField(AppTheme.CreateFieldLabel("حد الائتمان:"),  _creditBox,  1, 2);

        // نقاط الولاء + الحالة
        var loyaltyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 0) };
        var loyaltyTitle = AppTheme.CreateFieldLabel("نقاط الولاء المتراكمة:");
        loyaltyTitle.Dock = DockStyle.Top;
        _pointsLbl.Dock      = DockStyle.Fill;
        _pointsLbl.Font      = new Font("Tahoma", 13F, FontStyle.Bold);
        _pointsLbl.ForeColor = AppTheme.Success;
        _pointsLbl.TextAlign = ContentAlignment.MiddleRight;
        loyaltyPanel.Controls.Add(_pointsLbl);
        loyaltyPanel.Controls.Add(loyaltyTitle);
        tbl.Controls.Add(loyaltyPanel, 0, 3);

        _activeChk.Dock      = DockStyle.None;
        _activeChk.Location  = new Point(8, 20);
        _activeChk.Font      = AppTheme.BodyFont;
        _activeChk.ForeColor = AppTheme.DarkText;
        var activeWrap = new Panel { Dock = DockStyle.Fill };
        activeWrap.Controls.Add(_activeChk);
        tbl.Controls.Add(activeWrap, 1, 3);

        // قاعدة النقاط
        var rulesLbl = new Label
        {
            Text      = "📌 قاعدة الولاء: 1 نقطة لكل 10 جنيه • 10 نقاط = 1 جنيه خصم • الحد الأدنى للصرف: 100 نقطة",
            Dock      = DockStyle.Fill,
            Font      = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleRight
        };
        tbl.SetColumnSpan(rulesLbl, 2);
        // نضيف rulesLbl في صف 4
        var rulesWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 0) };
        rulesWrap.Controls.Add(rulesLbl);
        tbl.Controls.Add(rulesWrap, 0, 4);
        tbl.SetColumnSpan(rulesWrap, 2);

        card.Controls.Add(tbl);
        root.Controls.Add(card, 0, 1);

        // شريط الأزرار
        var btnBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 4, 0, 0)
        };
        AppTheme.StylePrimaryButton(_saveBtn);
        _saveBtn.Width = 140; _saveBtn.Margin = new Padding(6, 0, 0, 0);

        _feedbackLbl.AutoSize  = true;
        _feedbackLbl.Margin    = new Padding(12, 8, 0, 0);
        _feedbackLbl.Font      = AppTheme.BodyFont;
        _feedbackLbl.ForeColor = AppTheme.MutedText;

        btnBar.Controls.Add(_saveBtn);
        btnBar.Controls.Add(_feedbackLbl);
        root.Controls.Add(btnBar, 0, 2);

        Controls.Add(root);
    }

    private void LoadData()
    {
        var repo = new CustomerRepository();
        if (_isNew)
        {
            _codeBox.Text   = repo.NextCustomerCode();
            _creditBox.Text = "0";
            _pointsLbl.Text = "0 نقطة";
        }
        else
        {
            _codeBox.Text    = _customer.Code;
            _nameBox.Text    = _customer.Name;
            _phoneBox.Text   = _customer.Phone;
            _emailBox.Text   = _customer.Email;
            _addressBox.Text = _customer.Address;
            _creditBox.Text  = _customer.CreditLimit.ToString("N2");
            _pointsLbl.Text  = $"{_customer.LoyaltyPoints:N0} نقطة";
            _activeChk.Checked = _customer.IsActive;
        }
    }

    private void Save()
    {
        var name = _nameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        { ShowError("الاسم مطلوب."); _nameBox.Focus(); return; }

        var repo = new CustomerRepository();
        if (repo.NameExists(name, _customer.Id))
        { ShowError("يوجد عميل بهذا الاسم بالفعل."); _nameBox.Focus(); return; }

        if (!decimal.TryParse(_creditBox.Text, out decimal credit) || credit < 0)
        { ShowError("حد الائتمان يجب أن يكون رقماً موجباً."); _creditBox.Focus(); return; }

        _customer.Code        = _codeBox.Text.Trim();
        _customer.Name        = name;
        _customer.Phone       = _phoneBox.Text.Trim();
        _customer.Email       = _emailBox.Text.Trim();
        _customer.Address     = _addressBox.Text.Trim();
        _customer.CreditLimit = credit;
        _customer.IsActive    = _activeChk.Checked;

        try
        {
            if (_isNew) repo.Insert(_customer);
            else        repo.Update(_customer);
            ShowSuccess("✅ تم الحفظ بنجاح.");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { ShowError($"خطأ: {ex.Message}"); }
    }

    private void ShowSuccess(string m) { _feedbackLbl.ForeColor = AppTheme.Success; _feedbackLbl.Text = m; }
    private void ShowError(string m)   { _feedbackLbl.ForeColor = AppTheme.Danger;  _feedbackLbl.Text = m; }
}
