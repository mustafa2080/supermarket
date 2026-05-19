using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>TASK-016 — نموذج إضافة / تعديل عرض</summary>
internal sealed class PromotionDialog : Form
{
    private readonly ComboBox  _typeCombo;
    private readonly TextBox   _nameBox;
    private readonly TextBox   _discValueBox;
    private readonly TextBox   _buyQtyBox;
    private readonly TextBox   _getQtyBox;
    private readonly TextBox   _getPriceBox;
    private readonly ComboBox  _appliesToCombo;
    private readonly ComboBox  _itemCombo;
    private readonly ComboBox  _groupCombo;
    private readonly DateTimePicker _startPicker;
    private readonly DateTimePicker _endPicker;
    private readonly CheckBox  _activeChk;
    private readonly Button    _saveBtn;
    private readonly Label     _feedbackLbl;

    // صفوف الخيارات الديناميكية
    private readonly Panel     _pctRow;
    private readonly Panel     _buyXRow;
    private readonly Panel     _bogoRow;
    private readonly Panel     _targetRow;
    private readonly Panel     _itemRow;
    private readonly Panel     _groupRow;

    private readonly Promotion _promo;
    private readonly bool      _isNew;

    public PromotionDialog(Promotion? existing = null)
    {
        _typeCombo      = AppTheme.CreateComboBox();
        _nameBox        = AppTheme.CreateTextBox();
        _discValueBox   = AppTheme.CreateTextBox();
        _buyQtyBox      = AppTheme.CreateTextBox();
        _getQtyBox      = AppTheme.CreateTextBox();
        _getPriceBox    = AppTheme.CreateTextBox();
        _appliesToCombo = AppTheme.CreateComboBox();
        _itemCombo      = AppTheme.CreateComboBox();
        _groupCombo     = AppTheme.CreateComboBox();
        _startPicker    = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _endPicker      = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _activeChk      = new CheckBox { Text = "نشط", Checked = true };
        _saveBtn        = new Button { Text = "💾 حفظ" };
        _feedbackLbl    = new Label();

        _pctRow    = MakeRow(); _buyXRow = MakeRow(); _bogoRow = MakeRow();
        _targetRow = MakeRow(); _itemRow = MakeRow(); _groupRow = MakeRow();

        _isNew = existing is null;
        _promo = existing ?? new Promotion();

        Text              = _isNew ? "عرض جديد" : $"تعديل: {_promo.Name}";
        Size              = new Size(580, 560);
        MinimumSize       = new Size(560, 520);
        StartPosition     = FormStartPosition.CenterParent;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        MaximizeBox       = false;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;

        BuildUI();
        LoadLookups();
        LoadData();

        _typeCombo.SelectedIndexChanged     += (_, _) => UpdateVisibility();
        _appliesToCombo.SelectedIndexChanged += (_, _) => UpdateVisibility();
        _saveBtn.Click                       += (_, _) => Save();
    }

    private static Panel MakeRow() => new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };

    private void BuildUI()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(14), BackColor = AppTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        var titleLbl = new Label
        {
            Text = _isNew ? "➕ إضافة عرض جديد" : "✏️ تعديل العرض",
            Dock = DockStyle.Fill, Font = AppTheme.TitleFont,
            ForeColor = AppTheme.Primary, TextAlign = ContentAlignment.MiddleRight
        };
        root.Controls.Add(titleLbl, 0, 0);

        // الحقول في Panel قابل للتمرير
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack  = new Panel { Dock = DockStyle.Top, AutoSize = true };

        void AddField(Panel row, string label, Control ctrl)
        {
            var lbl = AppTheme.CreateFieldLabel(label);
            lbl.Dock = DockStyle.Top;
            ctrl.Dock = DockStyle.Fill;
            var wrap = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(4, 0, 4, 0) };
            wrap.Controls.Add(ctrl); wrap.Controls.Add(lbl);
            row.Controls.Add(wrap);
        }

        // صف اسم العرض
        var nameRow = MakeRow();
        AddField(nameRow, "اسم العرض: *", _nameBox);

        // صف نوع العرض
        var typeRow = MakeRow();
        _typeCombo.Items.AddRange(new[] { "خصم نسبي %", "اشتري X احصل على Y", "BOGO (اشتري 1 واحصل على 1)" });
        AddField(typeRow, "نوع العرض: *", _typeCombo);

        // صف الخصم %
        AddField(_pctRow, "نسبة الخصم %:", _discValueBox);

        // صفوف buy_x_get_y
        var bxgy = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
        var bxgyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        bxgyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bxgyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bxgyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bxgyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _buyQtyBox.Dock = DockStyle.Fill; _getQtyBox.Dock = DockStyle.Fill; _getPriceBox.Dock = DockStyle.Fill;
        bxgyLayout.Controls.Add(AppTheme.CreateFieldLabel("اشتري كمية:"), 0, 0);
        bxgyLayout.Controls.Add(_buyQtyBox, 1, 0);
        bxgyLayout.Controls.Add(AppTheme.CreateFieldLabel("احصل على:"), 2, 0);
        bxgyLayout.Controls.Add(_getQtyBox, 3, 0);
        bxgy.Controls.Add(bxgyLayout);
        _buyXRow.Controls.Add(bxgy);
        AddField(_buyXRow, "السعر المخفَّض (0=مجاناً):", _getPriceBox);

        // صف BOGO
        var bogoLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        bogoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bogoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bogoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        bogoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        var bogoB = AppTheme.CreateTextBox(); var bogoG = AppTheme.CreateTextBox();
        bogoB.Dock = DockStyle.Fill; bogoG.Dock = DockStyle.Fill;
        bogoB.Name = "bogoB"; bogoG.Name = "bogoG";
        bogoLayout.Controls.Add(AppTheme.CreateFieldLabel("اشتري:"), 0, 0);
        bogoLayout.Controls.Add(bogoB,  1, 0);
        bogoLayout.Controls.Add(AppTheme.CreateFieldLabel("احصل على:"), 2, 0);
        bogoLayout.Controls.Add(bogoG,  3, 0);
        _bogoRow.Controls.Add(bogoLayout);
        _bogoRow.Controls.Add(bogoLayout);

        // صف ينطبق على
        AddField(_targetRow, "ينطبق على:", _appliesToCombo);
        _appliesToCombo.Items.AddRange(new[] { "صنف محدد", "مجموعة أصناف" });

        // صف الصنف
        AddField(_itemRow, "اختر الصنف:", _itemCombo);

        // صف المجموعة
        AddField(_groupRow, "اختر المجموعة:", _groupCombo);

        // صف التواريخ
        var dateRow = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
        var dateTbl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        dateTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        dateTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        dateTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        dateTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _startPicker.Dock = DockStyle.Fill; _endPicker.Dock = DockStyle.Fill;
        dateTbl.Controls.Add(AppTheme.CreateFieldLabel("من:"), 0, 0);
        dateTbl.Controls.Add(_startPicker, 1, 0);
        dateTbl.Controls.Add(AppTheme.CreateFieldLabel("إلى:"), 2, 0);
        dateTbl.Controls.Add(_endPicker, 3, 0);
        dateRow.Controls.Add(dateTbl);

        // صف الحالة
        var activeRow = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        _activeChk.Dock = DockStyle.Fill; _activeChk.Font = AppTheme.BodyFont; _activeChk.ForeColor = AppTheme.DarkText;
        activeRow.Controls.Add(_activeChk);

        // ترتيب الصفوف (مقلوب في DockStyle.Top)
        foreach (var r in new Control[] { activeRow, dateRow, _groupRow, _itemRow, _targetRow, _bogoRow, _buyXRow, _pctRow, typeRow, nameRow })
            stack.Controls.Add(r);
        scroll.Controls.Add(stack);
        root.Controls.Add(scroll, 0, 1);

        // شريط الأزرار
        var btnBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, Padding = new Padding(0, 4, 0, 0)
        };
        AppTheme.StylePrimaryButton(_saveBtn); _saveBtn.Width = 140; _saveBtn.Margin = new Padding(6, 0, 0, 0);
        _feedbackLbl.AutoSize = true; _feedbackLbl.Margin = new Padding(12, 8, 0, 0);
        _feedbackLbl.Font = AppTheme.BodyFont; _feedbackLbl.ForeColor = AppTheme.MutedText;
        btnBar.Controls.Add(_saveBtn); btnBar.Controls.Add(_feedbackLbl);
        root.Controls.Add(btnBar, 0, 2);

        Controls.Add(root);
    }

    private void LoadLookups()
    {
        try
        {
            // الأصناف
            _itemCombo.Items.Clear();
            _itemCombo.Items.Add(new LookupItem(0, "-- اختر صنفاً --"));
            var items = new ItemRepository().GetAll();
            foreach (var it in items) _itemCombo.Items.Add(new LookupItem(it.Id, it.NameAr));
            _itemCombo.SelectedIndex = 0;

            // المجموعات
            _groupCombo.Items.Clear();
            _groupCombo.Items.Add(new LookupItem(0, "-- اختر مجموعة --"));
            var groups = new CategoryRepository().GetAllGroups();
            foreach (var g in groups) _groupCombo.Items.Add(new LookupItem(g.Id, g.NameAr));
            _groupCombo.SelectedIndex = 0;
        }
        catch { /* تجاهل في وضع التصميم */ }
    }

    private void LoadData()
    {
        _nameBox.Text = _promo.Name;
        _typeCombo.SelectedIndex = _promo.Type switch
        {
            "buy_x_get_y" => 1,
            "bogo"        => 2,
            _             => 0
        };
        _discValueBox.Text = _promo.DiscountValue?.ToString("N2") ?? "";
        _buyQtyBox.Text    = _promo.BuyQuantity?.ToString() ?? "1";
        _getQtyBox.Text    = _promo.GetQuantity?.ToString() ?? "1";
        _getPriceBox.Text  = _promo.GetPrice?.ToString("N2") ?? "0";
        _appliesToCombo.SelectedIndex = _promo.AppliesTo == "group" ? 1 : 0;
        _startPicker.Value = _promo.StartDate < new DateTime(2000, 1, 1) ? DateTime.Today : _promo.StartDate;
        _endPicker.Value   = _promo.EndDate   < new DateTime(2000, 1, 1) ? DateTime.Today.AddMonths(1) : _promo.EndDate;
        _activeChk.Checked = _promo.IsActive;

        // اختيار الصنف/المجموعة
        if (_promo.ItemId.HasValue)
            for (int i = 0; i < _itemCombo.Items.Count; i++)
                if (_itemCombo.Items[i] is LookupItem li && li.Id == _promo.ItemId) { _itemCombo.SelectedIndex = i; break; }
        if (_promo.GroupId.HasValue)
            for (int i = 0; i < _groupCombo.Items.Count; i++)
                if (_groupCombo.Items[i] is LookupItem li && li.Id == _promo.GroupId) { _groupCombo.SelectedIndex = i; break; }

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        string type = _typeCombo.SelectedIndex switch { 1 => "buy_x_get_y", 2 => "bogo", _ => "percentage" };
        _pctRow.Visible    = type == "percentage";
        _buyXRow.Visible   = type == "buy_x_get_y";
        _bogoRow.Visible   = type == "bogo";
        bool isGroup = _appliesToCombo.SelectedIndex == 1;
        _itemRow.Visible  = !isGroup;
        _groupRow.Visible = isGroup;
    }

    private void Save()
    {
        var name = _nameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { ShowError("اسم العرض مطلوب."); return; }
        if (_startPicker.Value > _endPicker.Value) { ShowError("تاريخ البداية يجب أن يكون قبل تاريخ الانتهاء."); return; }

        string type = _typeCombo.SelectedIndex switch { 1 => "buy_x_get_y", 2 => "bogo", _ => "percentage" };
        bool   isGroup = _appliesToCombo.SelectedIndex == 1;

        _promo.Name      = name;
        _promo.Type      = type;
        _promo.AppliesTo = isGroup ? "group" : "item";
        _promo.StartDate = _startPicker.Value.Date;
        _promo.EndDate   = _endPicker.Value.Date;
        _promo.IsActive  = _activeChk.Checked;

        if (type == "percentage")
        {
            if (!decimal.TryParse(_discValueBox.Text, out decimal pct) || pct <= 0 || pct > 100)
            { ShowError("نسبة الخصم يجب أن تكون بين 1 و 100."); return; }
            _promo.DiscountValue = pct;
            _promo.BuyQuantity = _promo.GetQuantity = null; _promo.GetPrice = null;
        }
        else if (type == "buy_x_get_y")
        {
            if (!int.TryParse(_buyQtyBox.Text, out int bq) || bq < 1) { ShowError("كمية الشراء غير صالحة."); return; }
            if (!int.TryParse(_getQtyBox.Text, out int gq) || gq < 1) { ShowError("كمية الحصول غير صالحة."); return; }
            decimal.TryParse(_getPriceBox.Text, out decimal gp);
            _promo.BuyQuantity   = bq;
            _promo.GetQuantity   = gq;
            _promo.GetPrice      = gp > 0 ? gp : null;
            _promo.DiscountValue = null;
        }
        else // bogo
        {
            _promo.BuyQuantity   = 1;
            _promo.GetQuantity   = 1;
            _promo.GetPrice      = null;
            _promo.DiscountValue = null;
        }

        // الصنف / المجموعة
        if (isGroup)
        {
            _promo.ItemId  = null;
            _promo.GroupId = _groupCombo.SelectedItem is LookupItem gl && gl.Id > 0 ? gl.Id : null;
            if (_promo.GroupId is null) { ShowError("اختر مجموعة."); return; }
        }
        else
        {
            _promo.GroupId = null;
            _promo.ItemId  = _itemCombo.SelectedItem is LookupItem il && il.Id > 0 ? il.Id : null;
            if (_promo.ItemId is null) { ShowError("اختر صنفاً."); return; }
        }

        try
        {
            var repo = new PromotionRepository();
            if (_isNew) repo.Insert(_promo);
            else        repo.Update(_promo);
            ShowSuccess("✅ تم الحفظ بنجاح.");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { ShowError($"خطأ: {ex.Message}"); }
    }

    private void ShowSuccess(string m) { _feedbackLbl.ForeColor = AppTheme.Success; _feedbackLbl.Text = m; }
    private void ShowError(string m)   { _feedbackLbl.ForeColor = AppTheme.Danger;  _feedbackLbl.Text = m; }
}

// ── LookupItem helper ────────────────────────────────────────
file sealed class LookupItem
{
    public int    Id   { get; }
    public string Name { get; }
    public LookupItem(int id, string name) { Id = id; Name = name; }
    public override string ToString() => Name;
}
