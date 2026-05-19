using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

// ══════════════════════════════════════════════════════════════
//  Dialog إضافة / تعديل مورد
// ══════════════════════════════════════════════════════════════
internal sealed class SupplierDialog : Form
{
    private readonly TextBox        _nameBox;
    private readonly TextBox        _codeBox;
    private readonly TextBox        _phoneBox;
    private readonly TextBox        _mobileBox;
    private readonly TextBox        _emailBox;
    private readonly TextBox        _addressBox;
    private readonly TextBox        _taxNumberBox;
    private readonly NumericUpDown  _creditLimitBox;
    private readonly TextBox        _notesBox;
    private readonly CheckBox       _activeCheck;
    private readonly Label          _feedbackLabel;
    private readonly Button         _saveBtn;
    private readonly Supplier?      _editSupplier;

    public SupplierDialog(Supplier? editSupplier = null)
    {
        _editSupplier   = editSupplier;
        _nameBox        = AppTheme.CreateTextBox("إلزامي");
        _codeBox        = AppTheme.CreateTextBox("اختياري — يُولَّد تلقائياً");
        _phoneBox       = AppTheme.CreateTextBox();
        _mobileBox      = AppTheme.CreateTextBox();
        _emailBox       = AppTheme.CreateTextBox();
        _addressBox     = AppTheme.CreateTextBox();
        _taxNumberBox   = AppTheme.CreateTextBox();
        _creditLimitBox = AppTheme.CreateNumericInput(2, 1000m);
        _creditLimitBox.Maximum = 10_000_000;
        _notesBox       = AppTheme.CreateTextBox("ملاحظات اختيارية...");
        _notesBox.Multiline = true;
        _notesBox.Height    = 55;
        _activeCheck    = new CheckBox
        {
            Text = "نشط", Checked = true, Font = AppTheme.BodyFont,
            ForeColor = AppTheme.DarkText, AutoSize = true
        };
        _feedbackLabel  = new Label();
        _saveBtn        = new Button { Text = editSupplier is null ? "حفظ المورد" : "حفظ التعديلات" };

        Text              = editSupplier is null ? "إضافة مورد جديد" : $"تعديل: {editSupplier.Name}";
        Size              = new Size(640, 560);
        MinimumSize       = new Size(640, 560);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;

        BuildUI();

        if (editSupplier is not null) FillForm(editSupplier);
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(16), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));

        root.Controls.Add(BuildFormCard(), 0, 0);
        root.Controls.Add(BuildActionBar(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildFormCard()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 8, 12, 8);

        var title = new Label
        {
            Dock = DockStyle.Top, Height = 30,
            Font = AppTheme.SectionFont, ForeColor = AppTheme.Primary,
            Text = "بيانات المورد", TextAlign = ContentAlignment.MiddleRight
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2,
            AutoScroll = true, Padding = new Padding(0, 4, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        AddField(grid, 0, 0, "اسم المورد *",    _nameBox);
        AddField(grid, 1, 0, "كود المورد",       _codeBox);
        AddField(grid, 0, 1, "التليفون",         _phoneBox);
        AddField(grid, 1, 1, "الموبايل",         _mobileBox);
        AddField(grid, 0, 2, "البريد الإلكتروني",_emailBox);
        AddField(grid, 1, 2, "الرقم الضريبي",   _taxNumberBox);
        AddField(grid, 0, 3, "العنوان",          _addressBox);
        AddField(grid, 1, 3, "حد الائتمان (جنيه)", _creditLimitBox);
        AddField(grid, 0, 4, "ملاحظات",          _notesBox);

        var statusPanel = new Panel { Dock = DockStyle.Top, Height = 64 };
        var statusLabel = AppTheme.CreateFieldLabel("الحالة");
        statusLabel.Dock = DockStyle.Top;
        _activeCheck.Margin = new Padding(0, 4, 0, 0);
        statusPanel.Controls.Add(_activeCheck);
        statusPanel.Controls.Add(statusLabel);
        grid.Controls.Add(statusPanel, 1, 4);
        grid.SetRow(statusPanel, 4);
        grid.SetColumn(statusPanel, 1);

        card.Controls.Add(grid);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildActionBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = AppTheme.Background,
            Padding = new Padding(0, 8, 0, 0)
        };
        var cancelBtn = new Button { Text = "إلغاء", Width = 100 };
        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(cancelBtn);
        _saveBtn.Width  = 160;
        _saveBtn.Margin = new Padding(8, 0, 8, 0);
        _saveBtn.Click   += (_, _) => Save();
        cancelBtn.Click  += (_, _) => Close();
        bar.Controls.Add(_saveBtn);
        bar.Controls.Add(cancelBtn);
        return bar;
    }

    private Control BuildFooter()
    {
        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;
        _feedbackLabel.Text      = "* الاسم حقل إلزامي — باقي الحقول اختيارية.";
        return _feedbackLabel;
    }

    private void FillForm(Supplier s)
    {
        _nameBox.Text        = s.Name;
        _codeBox.Text        = s.Code;
        _phoneBox.Text       = s.Phone;
        _mobileBox.Text      = s.Mobile;
        _emailBox.Text       = s.Email;
        _addressBox.Text     = s.Address;
        _taxNumberBox.Text   = s.TaxNumber;
        _creditLimitBox.Value = s.CreditLimit > 0 ? s.CreditLimit : 0;
        _notesBox.Text       = s.Notes;
        _activeCheck.Checked = s.IsActive;
    }

    private void Save()
    {
        var name = _nameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("اسم المورد حقل إلزامي.");
            _nameBox.Focus();
            return;
        }

        try
        {
            var repo      = new SupplierRepository();
            int excludeId = _editSupplier?.Id ?? 0;

            if (repo.NameExists(name, excludeId))
            {
                ShowError($"المورد [{name}] موجود بالفعل في قاعدة البيانات.");
                return;
            }

            var supplier = new Supplier
            {
                Id          = _editSupplier?.Id ?? 0,
                Name        = name,
                Code        = _codeBox.Text.Trim(),
                Phone       = _phoneBox.Text.Trim(),
                Mobile      = _mobileBox.Text.Trim(),
                Email       = _emailBox.Text.Trim(),
                Address     = _addressBox.Text.Trim(),
                TaxNumber   = _taxNumberBox.Text.Trim(),
                CreditLimit = _creditLimitBox.Value,
                Notes       = _notesBox.Text.Trim(),
                IsActive    = _activeCheck.Checked
            };

            _saveBtn.Enabled = false;
            _saveBtn.Text    = "جاري الحفظ...";

            if (_editSupplier is null)
            {
                int newId = repo.Insert(supplier);
                AuditService.LogCreate("public.suppliers", newId);
            }
            else
            {
                repo.Update(supplier);
                AuditService.LogUpdate("public.suppliers", supplier.Id);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"فشل الحفظ: {ex.Message}");
            _saveBtn.Enabled = true;
            _saveBtn.Text    = _editSupplier is null ? "حفظ المورد" : "حفظ التعديلات";
        }
    }

    private void AddField(TableLayoutPanel grid, int col, int row, string label, Control input)
    {
        while (grid.RowStyles.Count <= row * 2 + 1)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        }
        grid.Controls.Add(AppTheme.CreateFieldLabel(label), col, row * 2);
        input.Dock = DockStyle.Top;
        grid.Controls.Add(input, col, row * 2 + 1);
    }

    private void ShowError(string m)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = m; }
}
