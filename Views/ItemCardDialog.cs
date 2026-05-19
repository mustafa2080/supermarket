using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-009 — Dialog إضافة / تعديل صنف مع ربط كامل بقاعدة البيانات
/// </summary>
internal sealed class ItemCardDialog : Form
{
    // ── الحقول ──────────────────────────────────────────────
    private readonly TextBox        _itemCodeBox;
    private readonly TextBox        _nameArBox;
    private readonly TextBox        _nameEnBox;
    private readonly TextBox        _barcodeBox;
    private readonly ComboBox       _groupCombo;
    private readonly ComboBox       _typeCombo;
    private readonly ComboBox       _unitCombo;
    private readonly ComboBox       _supplierCombo;
    private readonly NumericUpDown  _purchasePrice;
    private readonly NumericUpDown  _retailPrice;
    private readonly NumericUpDown  _wholesalePrice;
    private readonly NumericUpDown  _reorderPoint;
    private readonly NumericUpDown  _taxRate;
    private readonly RadioButton    _activeRadio;
    private readonly RadioButton    _inactiveRadio;
    private readonly TextBox        _notesBox;
    private readonly Label          _feedbackLabel;
    private readonly Button         _saveButton;

    // ── البيانات ─────────────────────────────────────────────
    private readonly Item?          _editItem;          // null = إضافة جديد
    private List<ItemGroup>         _groups    = new();
    private List<Unit>              _units     = new();
    private List<Supplier>          _suppliers = new();

    public ItemCardDialog(Item? editItem = null)
    {
        _editItem = editItem;

        _itemCodeBox    = AppTheme.CreateTextBox();
        _itemCodeBox.ReadOnly = true;
        _nameArBox      = AppTheme.CreateTextBox("إلزامي");
        _nameEnBox      = AppTheme.CreateTextBox();
        _barcodeBox     = AppTheme.CreateTextBox("EAN-13 أو Code-128");
        _groupCombo     = AppTheme.CreateComboBox();
        _typeCombo      = AppTheme.CreateComboBox();
        _unitCombo      = AppTheme.CreateComboBox();
        _supplierCombo  = AppTheme.CreateComboBox();
        _purchasePrice  = AppTheme.CreateNumericInput();
        _retailPrice    = AppTheme.CreateNumericInput();
        _wholesalePrice = AppTheme.CreateNumericInput();
        _reorderPoint   = AppTheme.CreateNumericInput(3, 0.5M);
        _taxRate        = AppTheme.CreateNumericInput();
        _activeRadio    = new RadioButton { Text = "نشط",     Checked = true, Font = AppTheme.BodyFont, ForeColor = AppTheme.Success, AutoSize = true };
        _inactiveRadio  = new RadioButton { Text = "غير نشط",                Font = AppTheme.BodyFont, ForeColor = AppTheme.MutedText, AutoSize = true };
        _notesBox       = AppTheme.CreateTextBox("ملاحظات اختيارية...");
        _notesBox.Height = 60;
        _notesBox.Multiline = true;
        _feedbackLabel  = new Label();
        _saveButton     = new Button { Text = editItem is null ? "حفظ الصنف" : "حفظ التعديلات" };

        Text              = editItem is null ? "إضافة صنف جديد" : $"تعديل: {editItem.NameAr}";
        Size              = new Size(820, 660);
        MinimumSize       = new Size(820, 660);
        StartPosition     = FormStartPosition.CenterParent;
        BackColor         = AppTheme.Background;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;

        BuildUI();
        LoadLookups();

        if (editItem is not null)
            FillFormFromItem(editItem);
    }

    // ── بناء الواجهة ─────────────────────────────────────────
    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(16), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        root.Controls.Add(BuildFormArea(), 0, 0);
        root.Controls.Add(BuildActionBar(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);
        Controls.Add(root);
    }

    private Control BuildFormArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2,
            BackColor = AppTheme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

        layout.Controls.Add(BuildMainCard(), 0, 0);
        layout.Controls.Add(BuildSideCard(), 1, 0);
        return layout;
    }

    private Control BuildMainCard()
    {
        var card  = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;

        var title = new Label
        {
            Dock = DockStyle.Top, Height = 30,
            Font = AppTheme.SectionFont, ForeColor = AppTheme.Primary,
            Text = "البيانات الأساسية والأسعار", TextAlign = ContentAlignment.MiddleRight
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2,
            AutoScroll = true, Padding = new Padding(0, 8, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // كود الصنف مع زر توليد
        var codePanel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Top, Height = 36 };
        codePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        codePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        var genBtn = new Button { Text = "توليد", Dock = DockStyle.Fill };
        AppTheme.StyleSecondaryButton(genBtn);
        genBtn.Click += (_, _) => GenerateCode();
        codePanel.Controls.Add(_itemCodeBox, 0, 0);
        codePanel.Controls.Add(genBtn, 1, 0);

        AddField(grid, 0, 0, "كود الصنف",       codePanel);
        AddField(grid, 1, 0, "الاسم (عربي) *",  _nameArBox);
        AddField(grid, 0, 1, "الاسم (إنجليزي)", _nameEnBox);
        AddField(grid, 1, 1, "الباركود",         BuildBarcodeField());
        AddField(grid, 0, 2, "المجموعة *",       _groupCombo);
        AddField(grid, 1, 2, "النوع",            _typeCombo);
        AddField(grid, 0, 3, "الوحدة *",         _unitCombo);
        AddField(grid, 1, 3, "المورد",           _supplierCombo);
        AddField(grid, 0, 4, "سعر الشراء",       _purchasePrice);
        AddField(grid, 1, 4, "سعر التجزئة *",   _retailPrice);
        AddField(grid, 0, 5, "سعر الجملة",      _wholesalePrice);
        AddField(grid, 1, 5, "نقطة إعادة الطلب", _reorderPoint);
        AddField(grid, 0, 6, "الضريبة %",        _taxRate);

        card.Controls.Add(grid);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildBarcodeField()
    {
        var panel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Top, Height = 36 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
        var checkBtn = new Button { Text = "✔", Dock = DockStyle.Fill, ToolTipText = "تحقق من تكرار الباركود" };
        AppTheme.StyleSecondaryButton(checkBtn);
        checkBtn.Click += (_, _) => CheckBarcode();
        panel.Controls.Add(_barcodeBox, 0, 0);
        panel.Controls.Add(checkBtn, 1, 0);
        return panel;
    }

    private Control BuildSideCard()
    {
        var card  = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;

        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, RowCount = 4
        };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Dock = DockStyle.Fill, Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Primary, Text = "الحالة والملاحظات",
            TextAlign = ContentAlignment.MiddleRight
        };

        // الحالة
        var statusPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(0, 4, 0, 0)
        };
        statusPanel.Controls.Add(AppTheme.CreateFieldLabel("الحالة"));
        statusPanel.Controls.Add(_activeRadio);
        statusPanel.Controls.Add(_inactiveRadio);

        // ملاحظات
        var notesPanel = new Panel { Dock = DockStyle.Fill };
        var notesLabel = AppTheme.CreateFieldLabel("ملاحظات");
        notesLabel.Dock = DockStyle.Top;
        _notesBox.Dock  = DockStyle.Fill;
        notesPanel.Controls.Add(_notesBox);
        notesPanel.Controls.Add(notesLabel);

        // feedback
        _feedbackLabel.Dock      = DockStyle.Fill;
        _feedbackLabel.Font      = AppTheme.SmallFont;
        _feedbackLabel.ForeColor = AppTheme.MutedText;
        _feedbackLabel.Text      = "* حقول إلزامية: الاسم العربي، المجموعة، الوحدة، سعر التجزئة.";
        _feedbackLabel.TextAlign = ContentAlignment.TopRight;

        container.Controls.Add(title, 0, 0);
        container.Controls.Add(statusPanel, 0, 1);
        container.Controls.Add(notesPanel, 0, 2);
        container.Controls.Add(_feedbackLabel, 0, 3);

        card.Controls.Add(container);
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

        var cancelButton = new Button { Text = "إلغاء", Width = 110 };
        AppTheme.StylePrimaryButton(_saveButton);
        AppTheme.StyleSecondaryButton(cancelButton);
        _saveButton.Width = 160;
        _saveButton.Margin = new Padding(8, 0, 8, 0);

        _saveButton.Click  += (_, _) => SaveItem();
        cancelButton.Click += (_, _) => Close();

        bar.Controls.Add(_saveButton);
        bar.Controls.Add(cancelButton);
        return bar;
    }

    private Control BuildFooter()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background };
        var hint = new Label
        {
            Dock = DockStyle.Fill, Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText, TextAlign = ContentAlignment.MiddleRight,
            Text = _editItem is null
                ? "سيتم حفظ الصنف في قاعدة البيانات مباشرةً."
                : $"آخر تعديل: {_editItem.CreatedAt:dd/MM/yyyy}  |  المخزون الحالي: {_editItem.CurrentStock:N2}"
        };
        panel.Controls.Add(hint);
        return panel;
    }

    // ── تحميل القوائم من DB ──────────────────────────────────
    private void LoadLookups()
    {
        try
        {
            var itemRepo     = new ItemRepository();
            var supplierRepo = new SupplierRepository();
            var categoryRepo = new CategoryRepository();

            _groups    = itemRepo.GetGroups();
            _units     = itemRepo.GetUnits();
            _suppliers = supplierRepo.GetAll();
            var types  = categoryRepo.GetAllTypes();

            // أنواع من DB
            _typeCombo.Items.Add(new ComboItem(0, "— بدون نوع —"));
            foreach (var t in types)
                _typeCombo.Items.Add(new ComboItem(t.Id, t.NameAr));
            _typeCombo.SelectedIndex = 0;

            // المجموعات
            _groupCombo.Items.Add(new ComboItem(0, "— اختر مجموعة —"));
            foreach (var g in _groups)
                _groupCombo.Items.Add(new ComboItem(g.Id, g.NameAr));
            _groupCombo.SelectedIndex = 0;

            // الوحدات
            _unitCombo.Items.Add(new ComboItem(0, "— اختر وحدة —"));
            foreach (var u in _units)
                _unitCombo.Items.Add(new ComboItem(u.Id, $"{u.NameAr}"));
            _unitCombo.SelectedIndex = 0;

            // الموردين
            _supplierCombo.Items.Add(new ComboItem(0, "— بدون مورد —"));
            foreach (var s in _suppliers)
                _supplierCombo.Items.Add(new ComboItem(s.Id, s.Name));
            _supplierCombo.SelectedIndex = 0;

            // كود تلقائي للصنف الجديد
            if (_editItem is null)
                _itemCodeBox.Text = itemRepo.NextItemCode();
        }
        catch (Exception ex)
        {
            ShowError($"خطأ في تحميل البيانات: {ex.Message}");
        }
    }

    // ── ملء النموذج عند التعديل ─────────────────────────────
    private void FillFormFromItem(Item item)
    {
        _itemCodeBox.Text = item.ItemCode;
        _nameArBox.Text   = item.NameAr;
        _nameEnBox.Text   = item.NameEn;
        _barcodeBox.Text  = item.Barcode;
        _purchasePrice.Value  = item.PurchasePrice;
        _retailPrice.Value    = item.RetailPrice;
        _wholesalePrice.Value = item.WholesalePrice;
        _reorderPoint.Value   = item.ReorderPoint;
        _taxRate.Value        = item.TaxRate;
        _notesBox.Text        = item.Notes;
        _activeRadio.Checked  = item.IsActive;
        _inactiveRadio.Checked = !item.IsActive;

        SelectComboById(_groupCombo,    item.GroupId);
        SelectComboById(_unitCombo,     item.UnitId);
        SelectComboById(_supplierCombo, item.SupplierId);
    }

    private void SelectComboById(ComboBox combo, int? id)
    {
        if (id is null) { combo.SelectedIndex = 0; return; }
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboItem ci && ci.Id == id.Value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    // ── توليد الكود ─────────────────────────────────────────
    private void GenerateCode()
    {
        try
        {
            _itemCodeBox.Text = new ItemRepository().NextItemCode();
            ShowInfo("تم توليد كود جديد للصنف.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    // ── فحص الباركود ────────────────────────────────────────
    private void CheckBarcode()
    {
        var barcode = _barcodeBox.Text.Trim();
        if (string.IsNullOrEmpty(barcode)) { ShowInfo("أدخل باركود أولاً."); return; }

        try
        {
            int excludeId = _editItem?.Id ?? 0;
            bool exists   = new ItemRepository().BarcodeExists(barcode, excludeId);
            if (exists)
                ShowError($"الباركود [{barcode}] مستخدم بالفعل.");
            else
                ShowSuccess("الباركود متاح ويمكن استخدامه ✔");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    // ── حفظ الصنف ───────────────────────────────────────────
    private void SaveItem()
    {
        // ── تحقق إلزامي ──
        if (string.IsNullOrWhiteSpace(_nameArBox.Text))
        {
            ShowError("الاسم العربي حقل إلزامي."); _nameArBox.Focus(); return;
        }
        if (_groupCombo.SelectedItem is not ComboItem gci || gci.Id == 0)
        {
            ShowError("يجب اختيار مجموعة."); _groupCombo.Focus(); return;
        }
        if (_unitCombo.SelectedItem is not ComboItem uci || uci.Id == 0)
        {
            ShowError("يجب اختيار وحدة."); _unitCombo.Focus(); return;
        }
        if (_retailPrice.Value <= 0)
        {
            ShowError("سعر التجزئة يجب أن يكون أكبر من صفر."); _retailPrice.Focus(); return;
        }

        // ── تحقق من تكرار الباركود ──
        var barcode = _barcodeBox.Text.Trim();
        if (!string.IsNullOrEmpty(barcode))
        {
            try
            {
                int excludeId = _editItem?.Id ?? 0;
                if (new ItemRepository().BarcodeExists(barcode, excludeId))
                {
                    ShowError($"الباركود [{barcode}] مستخدم بالفعل. غيّره أو اتركه فارغاً.");
                    _barcodeBox.Focus();
                    return;
                }
            }
            catch (Exception ex) { ShowError($"خطأ: {ex.Message}"); return; }
        }

        // ── بناء الـ Item ──
        var item = new Item
        {
            Id             = _editItem?.Id ?? 0,
            ItemCode       = _itemCodeBox.Text.Trim(),
            NameAr         = _nameArBox.Text.Trim(),
            NameEn         = _nameEnBox.Text.Trim(),
            Barcode        = barcode,
            BarcodeType    = "EAN-13",
            GroupId        = gci.Id,
            UnitId         = (_unitCombo.SelectedItem as ComboItem)?.Id is int uid && uid > 0 ? uid : null,
            SupplierId     = (_supplierCombo.SelectedItem as ComboItem)?.Id is int sid && sid > 0 ? sid : null,
            PurchasePrice  = _purchasePrice.Value,
            RetailPrice    = _retailPrice.Value,
            WholesalePrice = _wholesalePrice.Value,
            TaxRate        = _taxRate.Value,
            ReorderPoint   = _reorderPoint.Value,
            IsActive       = _activeRadio.Checked,
            Notes          = _notesBox.Text.Trim()
        };

        // ── حفظ في DB ──
        try
        {
            _saveButton.Enabled = false;
            _saveButton.Text    = "جاري الحفظ...";

            var repo = new ItemRepository();
            int userId = SessionContext.CurrentUser?.Id ?? 0;

            if (_editItem is null)
            {
                int newId = repo.Insert(item, userId);
                AuditService.LogCreate("public.items", newId);
            }
            else
            {
                repo.Update(item);
                AuditService.LogUpdate("public.items", item.Id);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"فشل الحفظ: {ex.Message}");
            _saveButton.Enabled = true;
            _saveButton.Text    = _editItem is null ? "حفظ الصنف" : "حفظ التعديلات";
        }
    }

    // ── Helpers ──────────────────────────────────────────────
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

    private void ShowSuccess(string msg) { _feedbackLabel.ForeColor = AppTheme.Success;   _feedbackLabel.Text = msg; }
    private void ShowError(string msg)   { _feedbackLabel.ForeColor = AppTheme.Danger;    _feedbackLabel.Text = msg; }
    private void ShowInfo(string msg)    { _feedbackLabel.ForeColor = AppTheme.MutedText; _feedbackLabel.Text = msg; }

    private record ComboItem(int Id, string Label) { public override string ToString() => Label; }
}
