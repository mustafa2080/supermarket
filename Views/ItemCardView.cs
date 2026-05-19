using System.Drawing;
using supermarket.Theme;

namespace supermarket.Views;

internal sealed class ItemCardView : UserControl
{
    private readonly TextBox _itemCodeTextBox;
    private readonly TextBox _nameArTextBox;
    private readonly TextBox _nameEnTextBox;
    private readonly TextBox _barcodeTextBox;
    private readonly ComboBox _groupComboBox;
    private readonly ComboBox _typeComboBox;
    private readonly ComboBox _unitComboBox;
    private readonly ComboBox _supplierComboBox;
    private readonly NumericUpDown _purchasePriceInput;
    private readonly NumericUpDown _retailPriceInput;
    private readonly NumericUpDown _wholesalePriceInput;
    private readonly NumericUpDown _reorderPointInput;
    private readonly NumericUpDown _taxRateInput;
    private RadioButton _activeRadio;
    private RadioButton _inactiveRadio;
    private Label _feedbackLabel;
    private int _nextItemSequence = 1;

    public ItemCardView()
    {
        _itemCodeTextBox = AppTheme.CreateTextBox();
        _itemCodeTextBox.ReadOnly = true;
        _nameArTextBox = AppTheme.CreateTextBox("الاسم العربي إلزامي");
        _nameEnTextBox = AppTheme.CreateTextBox();
        _barcodeTextBox = AppTheme.CreateTextBox("EAN-13 أو Code-128");
        _groupComboBox = AppTheme.CreateComboBox();
        _typeComboBox = AppTheme.CreateComboBox();
        _unitComboBox = AppTheme.CreateComboBox();
        _supplierComboBox = AppTheme.CreateComboBox();
        _purchasePriceInput = AppTheme.CreateNumericInput();
        _retailPriceInput = AppTheme.CreateNumericInput();
        _wholesalePriceInput = AppTheme.CreateNumericInput();
        _reorderPointInput = AppTheme.CreateNumericInput(3, 0.5M);
        _taxRateInput = AppTheme.CreateNumericInput();
        _activeRadio = new RadioButton();
        _inactiveRadio = new RadioButton();
        _feedbackLabel = new Label();

        Dock = DockStyle.Fill;
        BackColor = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
            BackColor = AppTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

        root.Controls.Add(BuildToolbar(), 0, 0);
        root.Controls.Add(BuildFormArea(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(root);

        ConfigureFormValues();
    }

    private Control BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 4, 0, 4)
        };

        var saveButton = new Button { Text = "حفظ", Width = 110 };
        var resetButton = new Button { Text = "إلغاء", Width = 110 };
        var printButton = new Button { Text = "طباعة", Width = 110 };

        AppTheme.StylePrimaryButton(saveButton);
        AppTheme.StyleSecondaryButton(resetButton);
        AppTheme.StyleSecondaryButton(printButton);

        saveButton.Click += (_, _) => SaveItem();
        resetButton.Click += (_, _) => ResetForm();
        printButton.Click += (_, _) => ShowInfo("الطباعة ستُربط لاحقاً مع نظام التقارير والباركود.");

        toolbar.Controls.Add(saveButton);
        toolbar.Controls.Add(resetButton);
        toolbar.Controls.Add(printButton);
        return toolbar;
    }

    private Control BuildFormArea()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 8, 0, 8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

        layout.Controls.Add(BuildMainFieldsCard(), 0, 0);
        layout.Controls.Add(BuildSideCard(), 1, 0);

        return layout;
    }

    private Control BuildMainFieldsCard()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Primary,
            Text = "البيانات الأساسية والأسعار",
            TextAlign = ContentAlignment.MiddleRight
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoScroll = true,
            Padding = new Padding(0, 12, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        AddField(grid, 0, 0, "كود الصنف", BuildCodeField());
        AddField(grid, 1, 0, "الاسم (عربي)", _nameArTextBox);
        AddField(grid, 0, 1, "الاسم (إنجليزي)", _nameEnTextBox);
        AddField(grid, 1, 1, "الباركود", _barcodeTextBox);
        AddField(grid, 0, 2, "المجموعة", _groupComboBox);
        AddField(grid, 1, 2, "النوع", _typeComboBox);
        AddField(grid, 0, 3, "الوحدة", _unitComboBox);
        AddField(grid, 1, 3, "المورد", _supplierComboBox);
        AddField(grid, 0, 4, "سعر الشراء", _purchasePriceInput);
        AddField(grid, 1, 4, "سعر التجزئة", _retailPriceInput);
        AddField(grid, 0, 5, "سعر الجملة", _wholesalePriceInput);
        AddField(grid, 1, 5, "نقطة إعادة الطلب", _reorderPointInput);
        AddField(grid, 0, 6, "الضريبة %", _taxRateInput);

        card.Controls.Add(grid);
        card.Controls.Add(title);
        return card;
    }

    private Control BuildCodeField()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Height = 36
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));

        var generateButton = new Button
        {
            Text = "توليد",
            Dock = DockStyle.Fill
        };
        AppTheme.StyleSecondaryButton(generateButton);
        generateButton.Click += (_, _) => GenerateItemCode();

        panel.Controls.Add(_itemCodeTextBox, 0, 0);
        panel.Controls.Add(generateButton, 1, 0);
        return panel;
    }

    private Control BuildSideCard()
    {
        var card = AppTheme.CreateCard();
        card.Dock = DockStyle.Fill;

        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4
        };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Primary,
            Text = "الصورة والحالة",
            TextAlign = ContentAlignment.MiddleRight
        };

        var imagePlaceholder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorTranslator.FromHtml("#EBF5FB"),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16)
        };

        var imageText = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.MutedText,
            Text = "صورة الصنف\r\nسيتم ربط اختيار ورفع الصورة في المرحلة التالية.",
            TextAlign = ContentAlignment.MiddleCenter
        };
        imagePlaceholder.Controls.Add(imageText);

        var statusPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        _activeRadio = new RadioButton
        {
            Text = "نشط",
            Checked = true,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.Success,
            AutoSize = true
        };

        _inactiveRadio = new RadioButton
        {
            Text = "غير نشط",
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.MutedText,
            AutoSize = true
        };

        statusPanel.Controls.Add(AppTheme.CreateFieldLabel("الحالة"));
        statusPanel.Controls.Add(_activeRadio);
        statusPanel.Controls.Add(_inactiveRadio);

        var notesPanel = new Panel { Dock = DockStyle.Fill };
        AppTheme.StyleInfoPanel(notesPanel);

        _feedbackLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.DarkText,
            Text = "املأ الحقول الإلزامية: الاسم العربي، المجموعة، الوحدة، سعر الشراء، سعر التجزئة.",
            TextAlign = ContentAlignment.TopRight
        };
        notesPanel.Controls.Add(_feedbackLabel);

        container.Controls.Add(title, 0, 0);
        container.Controls.Add(imagePlaceholder, 0, 1);
        container.Controls.Add(statusPanel, 0, 2);
        container.Controls.Add(notesPanel, 0, 3);

        card.Controls.Add(container);
        return card;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface
        };

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText,
            Text = "المطلوب لاحقاً: ربط قاعدة البيانات، التحقق من تكرار الباركود، واستيراد Excel للأصناف.",
            TextAlign = ContentAlignment.MiddleRight
        };

        footer.Controls.Add(hint);
        return footer;
    }

    private void AddField(TableLayoutPanel grid, int column, int row, string labelText, Control input)
    {
        while (grid.RowStyles.Count <= row * 2 + 1)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        }

        grid.Controls.Add(AppTheme.CreateFieldLabel(labelText), column, row * 2);
        input.Dock = DockStyle.Top;
        grid.Controls.Add(input, column, row * 2 + 1);
    }

    private void ConfigureFormValues()
    {
        _groupComboBox.Items.AddRange(["غذاء", "منظفات", "مشروبات", "مجمدات", "ألبان ومشتقات"]);
        _typeComboBox.Items.AddRange(["محلي", "مستورد", "مجمد", "قابل للتلف"]);
        _unitComboBox.Items.AddRange(["قطعة", "كيلوجرام", "لتر", "كرتونة"]);
        _supplierComboBox.Items.AddRange(["بدون مورد", "مورد نقدي", "شركة الأغذية المتحدة", "مباشر من المصنع"]);

        _groupComboBox.SelectedIndex = 0;
        _typeComboBox.SelectedIndex = 0;
        _unitComboBox.SelectedIndex = 0;
        _supplierComboBox.SelectedIndex = 0;
        _taxRateInput.Value = 14;

        GenerateItemCode();
    }

    private void GenerateItemCode()
    {
        _itemCodeTextBox.Text = $"ITEM-{_nextItemSequence:0000}";
        _nextItemSequence++;
        ShowInfo("تم توليد كود جديد للصنف. يمكنك حفظه أو استخدامه كمرجع قبل الربط بقاعدة البيانات.");
    }

    private void SaveItem()
    {
        if (string.IsNullOrWhiteSpace(_nameArTextBox.Text))
        {
            ShowError("الاسم العربي للصنف حقل إلزامي.");
            _nameArTextBox.Focus();
            return;
        }

        if (_groupComboBox.SelectedIndex < 0 || _unitComboBox.SelectedIndex < 0)
        {
            ShowError("يجب اختيار المجموعة والوحدة قبل الحفظ.");
            return;
        }

        if (_retailPriceInput.Value <= 0)
        {
            ShowError("سعر التجزئة يجب أن يكون أكبر من صفر.");
            _retailPriceInput.Focus();
            return;
        }

        if (_purchasePriceInput.Value < 0)
        {
            ShowError("سعر الشراء غير صالح.");
            return;
        }

        var status = _activeRadio.Checked ? "نشط" : "غير نشط";
        ShowSuccess($"تم تجهيز الصنف للحفظ: {_nameArTextBox.Text} | الكود: {_itemCodeTextBox.Text} | الحالة: {status}");
    }

    private void ResetForm()
    {
        _nameArTextBox.Clear();
        _nameEnTextBox.Clear();
        _barcodeTextBox.Clear();
        _purchasePriceInput.Value = 0;
        _retailPriceInput.Value = 0;
        _wholesalePriceInput.Value = 0;
        _reorderPointInput.Value = 0;
        _taxRateInput.Value = 14;
        _activeRadio.Checked = true;
        _groupComboBox.SelectedIndex = 0;
        _typeComboBox.SelectedIndex = 0;
        _unitComboBox.SelectedIndex = 0;
        _supplierComboBox.SelectedIndex = 0;
        GenerateItemCode();
        ShowInfo("تمت إعادة تعيين النموذج لقيد صنف جديد.");
    }

    private void ShowSuccess(string message)
    {
        _feedbackLabel.ForeColor = AppTheme.Success;
        _feedbackLabel.Text = message;
    }

    private void ShowError(string message)
    {
        _feedbackLabel.ForeColor = AppTheme.Danger;
        _feedbackLabel.Text = message;
    }

    private void ShowInfo(string message)
    {
        _feedbackLabel.ForeColor = AppTheme.DarkText;
        _feedbackLabel.Text = message;
    }
}
