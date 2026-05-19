using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

// ══════════════════════════════════════════════════════════════
//  Dialog إضافة / تعديل مجموعة أصناف
// ══════════════════════════════════════════════════════════════
internal sealed class GroupDialog : Form
{
    private readonly TextBox   _nameArBox;
    private readonly TextBox   _nameEnBox;
    private readonly CheckBox  _activeCheck;
    private readonly Label     _feedbackLabel;
    private readonly Button    _saveBtn;
    private readonly ItemGroup? _editGroup;

    public GroupDialog(ItemGroup? editGroup = null)
    {
        _editGroup = editGroup;

        _nameArBox     = AppTheme.CreateTextBox("اسم المجموعة بالعربي *");
        _nameEnBox     = AppTheme.CreateTextBox("Group Name in English");
        _activeCheck   = new CheckBox
        {
            Text      = "نشطة",
            Checked   = true,
            Font      = AppTheme.BodyFont,
            ForeColor = AppTheme.DarkText,
            AutoSize  = true
        };
        _feedbackLabel = new Label();
        _saveBtn       = new Button { Text = editGroup is null ? "حفظ المجموعة" : "حفظ التعديلات" };

        Text              = editGroup is null ? "إضافة مجموعة جديدة" : $"تعديل: {editGroup.NameAr}";
        Size              = new Size(420, 310);
        MinimumSize       = new Size(420, 310);
        MaximumSize       = new Size(420, 310);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;

        BuildUI();

        if (editGroup is not null)
        {
            _nameArBox.Text    = editGroup.NameAr;
            _nameEnBox.Text    = editGroup.Name;
            _activeCheck.Checked = editGroup.IsActive;
        }
    }

    private void BuildUI()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(20, 16, 20, 12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));  // label nameAr
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // input nameAr
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));  // spacer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));  // label nameEn
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // input nameEn
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // checkbox
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // buttons

        _nameArBox.Dock = DockStyle.Top;
        _nameEnBox.Dock = DockStyle.Top;

        layout.Controls.Add(AppTheme.CreateFieldLabel("اسم المجموعة (عربي) *"), 0, 0);
        layout.Controls.Add(_nameArBox,  0, 1);
        layout.Controls.Add(new Panel(), 0, 2);
        layout.Controls.Add(AppTheme.CreateFieldLabel("اسم المجموعة (إنجليزي)"), 0, 3);
        layout.Controls.Add(_nameEnBox,  0, 4);
        layout.Controls.Add(_activeCheck, 0, 5);
        layout.Controls.Add(BuildActionRow(), 0, 6);

        card.Controls.Add(layout);
        Controls.Add(card);
    }

    private Control BuildActionRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        var cancelBtn = new Button { Text = "إلغاء", Width = 90, Dock = DockStyle.Fill };
        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(cancelBtn);
        _saveBtn.Dock    = DockStyle.Fill;
        _saveBtn.Click   += (_, _) => Save();
        cancelBtn.Click  += (_, _) => Close();

        panel.Controls.Add(_feedbackLabel, 0, 0);
        panel.Controls.Add(_saveBtn,       1, 0);
        panel.Controls.Add(cancelBtn,      2, 0);
        return panel;
    }

    private void Save()
    {
        var nameAr = _nameArBox.Text.Trim();
        var nameEn = _nameEnBox.Text.Trim();

        if (string.IsNullOrEmpty(nameAr))
        {
            _feedbackLabel.ForeColor = AppTheme.Danger;
            _feedbackLabel.Text      = "الاسم العربي إلزامي.";
            _nameArBox.Focus();
            return;
        }

        try
        {
            var repo      = new CategoryRepository();
            int excludeId = _editGroup?.Id ?? 0;

            if (repo.GroupNameExists(nameAr, excludeId))
            {
                _feedbackLabel.ForeColor = AppTheme.Danger;
                _feedbackLabel.Text      = $"المجموعة [{nameAr}] موجودة بالفعل.";
                return;
            }

            var group = new ItemGroup
            {
                Id       = _editGroup?.Id ?? 0,
                Name     = string.IsNullOrEmpty(nameEn) ? nameAr : nameEn,
                NameAr   = nameAr,
                IsActive = _activeCheck.Checked
            };

            if (_editGroup is null)
            {
                int newId = repo.InsertGroup(group);
                AuditService.LogCreate("public.item_groups", newId);
            }
            else
            {
                repo.UpdateGroup(group);
                AuditService.LogUpdate("public.item_groups", group.Id);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _feedbackLabel.ForeColor = AppTheme.Danger;
            _feedbackLabel.Text      = $"خطأ: {ex.Message}";
        }
    }
}

// ══════════════════════════════════════════════════════════════
//  Dialog إضافة / تعديل نوع صنف
// ══════════════════════════════════════════════════════════════
internal sealed class TypeDialog : Form
{
    private readonly TextBox   _nameArBox;
    private readonly TextBox   _nameEnBox;
    private readonly CheckBox  _activeCheck;
    private readonly Label     _feedbackLabel;
    private readonly Button    _saveBtn;
    private readonly ItemType?  _editType;

    public TypeDialog(ItemType? editType = null)
    {
        _editType = editType;

        _nameArBox     = AppTheme.CreateTextBox("اسم النوع بالعربي *");
        _nameEnBox     = AppTheme.CreateTextBox("Type Name in English");
        _activeCheck   = new CheckBox
        {
            Text      = "نشط",
            Checked   = true,
            Font      = AppTheme.BodyFont,
            ForeColor = AppTheme.DarkText,
            AutoSize  = true
        };
        _feedbackLabel = new Label();
        _saveBtn       = new Button { Text = editType is null ? "حفظ النوع" : "حفظ التعديلات" };

        Text              = editType is null ? "إضافة نوع جديد" : $"تعديل: {editType.NameAr}";
        Size              = new Size(420, 310);
        MinimumSize       = new Size(420, 310);
        MaximumSize       = new Size(420, 310);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;

        BuildUI();

        if (editType is not null)
        {
            _nameArBox.Text      = editType.NameAr;
            _nameEnBox.Text      = editType.Name;
            _activeCheck.Checked = editType.IsActive;
        }
    }

    private void BuildUI()
    {
        var card = AppTheme.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(20, 16, 20, 12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _nameArBox.Dock = DockStyle.Top;
        _nameEnBox.Dock = DockStyle.Top;

        layout.Controls.Add(AppTheme.CreateFieldLabel("اسم النوع (عربي) *"), 0, 0);
        layout.Controls.Add(_nameArBox,   0, 1);
        layout.Controls.Add(new Panel(),  0, 2);
        layout.Controls.Add(AppTheme.CreateFieldLabel("اسم النوع (إنجليزي)"), 0, 3);
        layout.Controls.Add(_nameEnBox,   0, 4);
        layout.Controls.Add(_activeCheck, 0, 5);
        layout.Controls.Add(BuildActionRow(), 0, 6);

        card.Controls.Add(layout);
        Controls.Add(card);
    }

    private Control BuildActionRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));

        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.TextAlign = ContentAlignment.MiddleRight;

        var cancelBtn = new Button { Text = "إلغاء", Width = 90, Dock = DockStyle.Fill };
        AppTheme.StylePrimaryButton(_saveBtn);
        AppTheme.StyleSecondaryButton(cancelBtn);
        _saveBtn.Dock   = DockStyle.Fill;
        _saveBtn.Click  += (_, _) => Save();
        cancelBtn.Click += (_, _) => Close();

        panel.Controls.Add(_feedbackLabel, 0, 0);
        panel.Controls.Add(_saveBtn,       1, 0);
        panel.Controls.Add(cancelBtn,      2, 0);
        return panel;
    }

    private void Save()
    {
        var nameAr = _nameArBox.Text.Trim();
        var nameEn = _nameEnBox.Text.Trim();

        if (string.IsNullOrEmpty(nameAr))
        {
            _feedbackLabel.ForeColor = AppTheme.Danger;
            _feedbackLabel.Text      = "الاسم العربي إلزامي.";
            _nameArBox.Focus();
            return;
        }

        try
        {
            var repo      = new CategoryRepository();
            int excludeId = _editType?.Id ?? 0;

            if (repo.TypeNameExists(nameAr, excludeId))
            {
                _feedbackLabel.ForeColor = AppTheme.Danger;
                _feedbackLabel.Text      = $"النوع [{nameAr}] موجود بالفعل.";
                return;
            }

            var type = new ItemType
            {
                Id       = _editType?.Id ?? 0,
                Name     = string.IsNullOrEmpty(nameEn) ? nameAr : nameEn,
                NameAr   = nameAr,
                IsActive = _activeCheck.Checked
            };

            if (_editType is null)
            {
                int newId = repo.InsertType(type);
                AuditService.LogCreate("public.item_types", newId);
            }
            else
            {
                repo.UpdateType(type);
                AuditService.LogUpdate("public.item_types", type.Id);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _feedbackLabel.ForeColor = AppTheme.Danger;
            _feedbackLabel.Text      = $"خطأ: {ex.Message}";
        }
    }
}
