using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-018 — نموذج إضافة/تعديل مستودع</summary>
internal sealed class WarehouseDialog : Form
{
    private readonly TextBox  _nameBox;
    private readonly TextBox  _locationBox;
    private readonly CheckBox _defaultChk;
    private readonly CheckBox _activeChk;
    private readonly Button   _saveBtn;
    private readonly Button   _cancelBtn;
    private readonly Label    _feedbackLabel;
    private readonly Warehouse? _existing;

    public WarehouseDialog(Warehouse? existing = null)
    {
        _existing = existing;
        Text            = existing is null ? "مستودع جديد" : "تعديل مستودع";
        StartPosition   = FormStartPosition.CenterParent;
        Size            = new Size(440, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        BackColor       = AppTheme.Background;
        Font            = AppTheme.BodyFont;
        RightToLeft     = RightToLeft.Yes;
        RightToLeftLayout = true;

        _nameBox       = AppTheme.CreateTextBox("اسم المستودع *");
        _locationBox   = AppTheme.CreateTextBox("الموقع (اختياري)");
        _defaultChk    = new CheckBox { Text = "مستودع افتراضي", AutoSize = true, Font = AppTheme.BodyFont };
        _activeChk     = new CheckBox { Text = "نشط", AutoSize = true, Checked = true, Font = AppTheme.BodyFont };
        _saveBtn       = new Button { Text = "💾 حفظ" };
        _cancelBtn     = new Button { Text = "إلغاء" };
        _feedbackLabel = new Label();

        if (existing is not null)
        {
            _nameBox.Text       = existing.Name;
            _locationBox.Text   = existing.Location;
            _defaultChk.Checked = existing.IsDefault;
            _activeChk.Checked  = existing.IsActive;
        }

        BuildUI();
        _saveBtn.Click   += (_, _) => Save();
        _cancelBtn.Click += (_, _) => DialogResult = DialogResult.Cancel;
    }

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6,
            Padding = new Padding(20), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        _nameBox.Dock     = DockStyle.Fill;
        _locationBox.Dock = DockStyle.Fill;

        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(_cancelBtn);
        _saveBtn.Width = 120; _cancelBtn.Width = 90;

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.Danger;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        btnRow.Controls.Add(_saveBtn);
        btnRow.Controls.Add(_cancelBtn);
        btnRow.Controls.Add(_feedbackLabel);

        root.Controls.Add(_nameBox,     0, 0);
        root.Controls.Add(_locationBox, 0, 1);
        root.Controls.Add(_defaultChk,  0, 2);
        root.Controls.Add(_activeChk,   0, 3);
        root.Controls.Add(new Panel(),  0, 4);
        root.Controls.Add(btnRow,       0, 5);
        Controls.Add(root);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text)) { _feedbackLabel.Text = "اسم المستودع مطلوب."; return; }
        try
        {
            var w = new Warehouse
            {
                Id        = _existing?.Id ?? 0,
                Name      = _nameBox.Text.Trim(),
                Location  = _locationBox.Text.Trim(),
                IsDefault = _defaultChk.Checked,
                IsActive  = _activeChk.Checked
            };
            new WarehouseRepository().Save(w);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { _feedbackLabel.Text = ex.Message; }
    }
}
