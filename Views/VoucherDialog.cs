using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>نموذج إنشاء سند صرف أو قبض — TASK-022</summary>
internal class VoucherDialog : Form
{
    public enum VoucherType { Payment, Receipt }

    private readonly TreasuryRepository _repo = new();
    private readonly VoucherType _type;

    private ComboBox _cmbSafe    = null!;
    private TextBox  _txtAmount  = null!;
    private ComboBox _cmbCat     = null!;   // expense item أو revenue source
    private TextBox  _txtDesc    = null!;
    private Label    _lblBalance = null!;
    private Button   _btnSave    = null!;
    private Button   _btnCancel  = null!;

    private List<Safe> _safes = new();

    public VoucherDialog(VoucherType type)
    {
        _type = type;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        bool isPay = _type == VoucherType.Payment;
        Text              = isPay ? "💸 سند صرف جديد" : "💰 سند قبض جديد";
        Size              = new Size(440, 340);
        StartPosition     = FormStartPosition.CenterParent;
        MinimizeBox       = false; MaximizeBox = false;
        FormBorderStyle   = FormBorderStyle.FixedDialog;
        RightToLeft       = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor         = AppTheme.Background;
        Font              = AppTheme.BodyFont;

        int y = 20;
        void Row(string lbl, Control ctrl)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(20, y + 3),
                AutoSize = true, ForeColor = AppTheme.DarkText });
            ctrl.Location = new Point(160, y); ctrl.Width = 240;
            Controls.Add(ctrl);
            y += 44;
        }

        // الخزينة
        _cmbSafe = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _cmbSafe.SelectedIndexChanged += OnSafeChanged;
        Row("الخزينة:", _cmbSafe);

        // رصيد الخزينة
        _lblBalance = new Label { Text = "الرصيد: —", AutoSize = true,
            Location = new Point(160, y - 26), ForeColor = AppTheme.MutedText,
            Font = AppTheme.SmallFont };
        Controls.Add(_lblBalance);

        // المبلغ
        _txtAmount = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        _txtAmount.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                e.Handled = true;
        };
        Row("المبلغ (ج.م):", _txtAmount);

        // التصنيف
        _cmbCat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        Row(isPay ? "بند المصروف:" : "مصدر الإيراد:", _cmbCat);

        // الوصف
        _txtDesc = new TextBox { BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText };
        Row("الوصف:", _txtDesc);

        // أزرار
        _btnSave = new Button
        {
            Text      = "✔ حفظ",
            Location  = new Point(160, y),
            Size      = new Size(110, 36),
            BackColor = isPay ? AppTheme.Danger : AppTheme.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += OnSave;

        _btnCancel = new Button
        {
            Text      = "إلغاء",
            Location  = new Point(290, y),
            Size      = new Size(110, 36),
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.DarkText,
            FlatStyle = FlatStyle.Flat
        };
        _btnCancel.FlatAppearance.BorderColor = AppTheme.Border;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
        ClientSize = new Size(440, y + 60);
    }

    private void LoadData()
    {
        try
        {
            _safes = _repo.GetSafes();
            foreach (var s in _safes)
                _cmbSafe.Items.Add(new SafeItem(s));
            if (_cmbSafe.Items.Count > 0) _cmbSafe.SelectedIndex = 0;

            if (_type == VoucherType.Payment)
            {
                foreach (var e in _repo.GetExpenseItems())
                    _cmbCat.Items.Add(new CatItem(e.Id, $"{e.GroupName} — {e.NameAr}"));
            }
            else
            {
                foreach (var rs in _repo.GetRevenueSources())
                    _cmbCat.Items.Add(new CatItem(rs.Id, rs.NameAr));
            }
            if (_cmbCat.Items.Count > 0) _cmbCat.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل البيانات:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSafeChanged(object? sender, EventArgs e)
    {
        if (_cmbSafe.SelectedItem is SafeItem si)
            _lblBalance.Text = $"الرصيد: {si.Balance:N2} ج.م";
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_cmbSafe.SelectedItem is not SafeItem safeItem)
        { MessageBox.Show("اختر الخزينة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        if (!decimal.TryParse(_txtAmount.Text.Trim(), out var amount) || amount <= 0)
        { MessageBox.Show("أدخل مبلغاً صحيحاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        if (_type == VoucherType.Payment && amount > safeItem.Balance)
        {
            MessageBox.Show($"المبلغ ({amount:N2}) يتجاوز رصيد الخزينة ({safeItem.Balance:N2} ج.م).",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int? catId = (_cmbCat.SelectedItem as CatItem)?.Id;
        string desc = _txtDesc.Text.Trim();

        try
        {
            if (_type == VoucherType.Payment)
                _repo.CreatePaymentVoucher(safeItem.Id, amount, catId, desc,
                    "manual", null, SessionContext.CurrentUser!.Id);
            else
                _repo.CreateReceiptVoucher(safeItem.Id, amount, catId, desc,
                    "manual", null, SessionContext.CurrentUser!.Id);

            MessageBox.Show(
                _type == VoucherType.Payment ? "✅ تم إنشاء سند الصرف." : "✅ تم إنشاء سند القبض.",
                "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الحفظ:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Helpers
    private record SafeItem(Safe S)
    {
        public int     Id      => S.Id;
        public decimal Balance => S.Balance;
        public override string ToString() => S.NameAr;
    }
    private record CatItem(int Id, string Label)
    {
        public override string ToString() => Label;
    }
}
