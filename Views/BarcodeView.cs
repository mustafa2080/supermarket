using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>شاشة الباركود — TASK-025: توليد + معاينة + طباعة جماعية</summary>
internal class BarcodeView : UserControl
{
    private readonly ItemRepository _itemRepo = new();

    private TabControl _tabs      = null!;
    private TabPage    _tabSelect = null!;
    private TabPage    _tabPrint  = null!;

    // ── تاب اختيار الأصناف ───────────────────────────────────
    private TextBox      _txtSearch    = null!;
    private ComboBox     _cmbGroup     = null!;
    private ComboBox     _cmbBarcodeType = null!;
    private DataGridView _gridItems    = null!;
    private Label        _lblItemsStat = null!;
    private Button       _btnAddSelected = null!;

    // ── تاب الطباعة ──────────────────────────────────────────
    private DataGridView _gridPrint    = null!;
    private Label        _lblPrintStat = null!;
    private PictureBox   _pbPreview    = null!;

    // إعدادات الملصق
    private CheckBox   _chkShowCompany = null!;
    private CheckBox   _chkShowName    = null!;
    private CheckBox   _chkShowPrice   = null!;
    private NumericUpDown _nudPerRow   = null!;
    private NumericUpDown _nudWidth    = null!;
    private NumericUpDown _nudHeight   = null!;
    private Button     _btnPreview     = null!;
    private Button     _btnPrint       = null!;
    private Button     _btnClear       = null!;

    private List<Item>         _allItems   = new();
    private List<BarcodeLabel> _printQueue = new();
    private List<ItemGroup>    _groups     = new();

    public BarcodeView()
    {
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;

        _tabs     = new TabControl { Dock = DockStyle.Fill, Font = AppTheme.BodyFont };
        _tabSelect = new TabPage("🔍 اختيار الأصناف")  { BackColor = AppTheme.Surface };
        _tabPrint  = new TabPage("🖨️ قائمة الطباعة")   { BackColor = AppTheme.Surface };

        _tabSelect.Controls.Add(BuildSelectPanel());
        _tabPrint.Controls.Add(BuildPrintPanel());

        _tabs.TabPages.Add(_tabSelect);
        _tabs.TabPages.Add(_tabPrint);
        Controls.Add(_tabs);
    }

    // ════ تاب اختيار الأصناف ═════════════════════════════════

    private Control BuildSelectPanel()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        // شريط الأدوات
        var toolbar = new Panel
        {
            Dock = DockStyle.Top, Height = 52,
            BackColor = AppTheme.Surface, Padding = new Padding(10, 10, 10, 10)
        };

        _txtSearch = new TextBox
        {
            Width = 200, Dock = DockStyle.Right,
            PlaceholderText = "بحث بالاسم / الكود / الباركود...",
            BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText
        };
        _txtSearch.TextChanged += (_, _) => FilterItems();

        _cmbGroup = new ComboBox
        {
            Width = 160, Dock = DockStyle.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface
        };
        _cmbGroup.SelectedIndexChanged += (_, _) => FilterItems();

        _cmbBarcodeType = new ComboBox
        {
            Width = 120, Dock = DockStyle.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = AppTheme.Surface
        };
        _cmbBarcodeType.Items.AddRange(new object[]
            { "EAN-13", "CODE-128", "QR" });
        _cmbBarcodeType.SelectedIndex = 0;

        _btnAddSelected = new Button
        {
            Text = "➕ أضف للطباعة", Width = 150, Dock = DockStyle.Left,
            BackColor = AppTheme.Success, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Height = 32
        };
        _btnAddSelected.FlatAppearance.BorderSize = 0;
        _btnAddSelected.Click += OnAddSelected;

        toolbar.Controls.Add(_btnAddSelected);
        toolbar.Controls.Add(new Label { Text = "النوع:", Dock = DockStyle.Right,
            AutoSize = true, ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleRight });
        toolbar.Controls.Add(_cmbBarcodeType);
        toolbar.Controls.Add(new Label { Text = "المجموعة:", Dock = DockStyle.Right,
            AutoSize = true, ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleRight });
        toolbar.Controls.Add(_cmbGroup);
        toolbar.Controls.Add(_txtSearch);

        // جدول الأصناف
        _gridItems = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface,
            GridColor = AppTheme.Border, BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            RowTemplate = { Height = 33 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Primary, ForeColor = Color.White,
                Font = AppTheme.SectionFont, Padding = new Padding(6)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
                Padding = new Padding(4),
                SelectionBackColor = Color.FromArgb(187, 222, 251),
                SelectionForeColor = AppTheme.DarkText
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Background }
        };

        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Code",    HeaderText = "الكود",         Width = 110 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name",    HeaderText = "اسم الصنف",    Width = 240 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Barcode", HeaderText = "الباركود",      Width = 140 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Type",    HeaderText = "النوع",          Width = 90  });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Price",   HeaderText = "سعر التجزئة",   Width = 110 });
        _gridItems.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Group",   HeaderText = "المجموعة",      Width = 130 });

        _gridItems.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _gridItems.Rows[e.RowIndex].Tag is Item item)
                AddToQueue(item, 1);
        };

        _lblItemsStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 28,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0),
            Text = "دبل كليك على صنف لإضافته — أو اختر أكثر من صنف واضغط «أضف للطباعة»"
        };

        pnl.Controls.Add(_gridItems);
        pnl.Controls.Add(_lblItemsStat);
        pnl.Controls.Add(toolbar);
        return pnl;
    }

    // ════ تاب قائمة الطباعة ══════════════════════════════════

    private Control BuildPrintPanel()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = AppTheme.Surface
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

        // ── يسار: جدول قائمة الطباعة ─────────────────────────
        var leftPnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        var toolbar = new Panel
        {
            Dock = DockStyle.Top, Height = 52,
            BackColor = AppTheme.Surface, Padding = new Padding(10, 10, 10, 10)
        };

        _btnClear = new Button
        {
            Text = "🗑️ مسح الكل", Width = 120, Dock = DockStyle.Right,
            BackColor = AppTheme.Danger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Height = 32
        };
        _btnClear.FlatAppearance.BorderSize = 0;
        _btnClear.Click += (_, _) =>
        {
            _printQueue.Clear();
            _gridPrint.Rows.Clear();
            _lblPrintStat.Text = "القائمة فارغة";
            _pbPreview.Image = null;
        };

        _btnPrint = new Button
        {
            Text = "🖨️ طباعة الكل", Width = 140, Dock = DockStyle.Right,
            BackColor = AppTheme.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Height = 32,
            Margin = new Padding(0, 0, 8, 0)
        };
        _btnPrint.FlatAppearance.BorderSize = 0;
        _btnPrint.Click += OnPrintAll;

        toolbar.Controls.Add(_btnClear);
        toolbar.Controls.Add(_btnPrint);

        _gridPrint = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface,
            GridColor = AppTheme.Border, BorderStyle = BorderStyle.None,
            RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowTemplate = { Height = 33 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Primary, ForeColor = Color.White,
                Font = AppTheme.SectionFont, Padding = new Padding(6)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.Surface, ForeColor = AppTheme.DarkText,
                Padding = new Padding(4),
                SelectionBackColor = Color.FromArgb(187, 222, 251),
                SelectionForeColor = AppTheme.DarkText
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                { BackColor = AppTheme.Background }
        };

        _gridPrint.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Name",    HeaderText = "اسم الصنف",  Width = 200 });
        _gridPrint.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Barcode", HeaderText = "الباركود",    Width = 130 });
        _gridPrint.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "BType",   HeaderText = "النوع",        Width = 80  });
        _gridPrint.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Price",   HeaderText = "السعر",        Width = 90  });

        // عمود عدد النسخ قابل للتعديل
        _gridPrint.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Copies",  HeaderText = "النسخ",        Width = 60,
              ReadOnly = false });

        // زر حذف
        var delBtn = new DataGridViewButtonColumn
        {
            Name = "Del", HeaderText = "", Width = 44,
            Text = "✖", UseColumnTextForButtonValue = true
        };
        _gridPrint.Columns.Add(delBtn);

        _gridPrint.CellValueChanged  += OnCopiesChanged;
        _gridPrint.CellClick         += OnDeleteRow;
        _gridPrint.SelectionChanged  += (_, _) => PreviewSelected();

        _lblPrintStat = new Label
        {
            Dock = DockStyle.Bottom, Height = 28,
            ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0)
        };

        leftPnl.Controls.Add(_gridPrint);
        leftPnl.Controls.Add(_lblPrintStat);
        leftPnl.Controls.Add(toolbar);

        // ── يمين: معاينة + إعدادات ───────────────────────────
        var rightPnl = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };

        var settingsCard = AppTheme.CreateCard();
        settingsCard.Dock    = DockStyle.Top;
        settingsCard.Height  = 200;
        settingsCard.Padding = new Padding(12, 10, 12, 10);

        var lblSettings = new Label
        {
            Text = "⚙️ إعدادات الملصق", Dock = DockStyle.Top, Height = 28,
            Font = AppTheme.SectionFont, ForeColor = AppTheme.Primary
        };

        var tblSettings = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5,
            BackColor = AppTheme.Surface
        };
        tblSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tblSettings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        _chkShowCompany = new CheckBox { Text = "اسم الشركة",  Checked = true,
            ForeColor = AppTheme.DarkText, AutoSize = true };
        _chkShowName    = new CheckBox { Text = "اسم الصنف",   Checked = true,
            ForeColor = AppTheme.DarkText, AutoSize = true };
        _chkShowPrice   = new CheckBox { Text = "السعر",       Checked = true,
            ForeColor = AppTheme.DarkText, AutoSize = true };

        _chkShowCompany.CheckedChanged += (_, _) => PreviewSelected();
        _chkShowName.CheckedChanged    += (_, _) => PreviewSelected();
        _chkShowPrice.CheckedChanged   += (_, _) => PreviewSelected();

        void AddSettingRow(string lbl, Control ctrl, int row)
        {
            tblSettings.Controls.Add(new Label { Text = lbl, AutoSize = true,
                ForeColor = AppTheme.MutedText, Font = AppTheme.SmallFont,
                Anchor = AnchorStyles.Right }, 0, row);
            tblSettings.Controls.Add(ctrl, 1, row);
        }

        _nudPerRow = new NumericUpDown { Minimum = 1, Maximum = 6, Value = 3,
            Width = 60, BackColor = AppTheme.Surface };
        _nudWidth  = new NumericUpDown { Minimum = 100, Maximum = 400, Value = 180,
            Width = 70, BackColor = AppTheme.Surface };
        _nudHeight = new NumericUpDown { Minimum = 60,  Maximum = 300, Value = 100,
            Width = 70, BackColor = AppTheme.Surface };

        _nudPerRow.ValueChanged += (_, _) => PreviewSelected();
        _nudWidth.ValueChanged  += (_, _) => PreviewSelected();
        _nudHeight.ValueChanged += (_, _) => PreviewSelected();

        tblSettings.Controls.Add(_chkShowCompany, 0, 0);
        tblSettings.Controls.Add(_chkShowName,    1, 0);
        tblSettings.Controls.Add(_chkShowPrice,   0, 1);
        AddSettingRow("ملصقات/صف:", _nudPerRow, 2);
        AddSettingRow("عرض (px):",  _nudWidth,  3);
        AddSettingRow("ارتفاع (px):", _nudHeight, 4);

        _btnPreview = new Button
        {
            Text = "👁️ معاينة", Dock = DockStyle.Bottom, Height = 36,
            BackColor = AppTheme.Secondary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnPreview.FlatAppearance.BorderSize = 0;
        _btnPreview.Click += (_, _) => PreviewSelected();

        settingsCard.Controls.Add(_btnPreview);
        settingsCard.Controls.Add(tblSettings);
        settingsCard.Controls.Add(lblSettings);

        // صندوق المعاينة
        var previewCard = AppTheme.CreateCard();
        previewCard.Dock    = DockStyle.Fill;
        previewCard.Padding = new Padding(8);

        var lblPrev = new Label
        {
            Text = "👁️ معاينة الملصق", Dock = DockStyle.Top, Height = 26,
            Font = AppTheme.SectionFont, ForeColor = AppTheme.Primary
        };

        _pbPreview = new PictureBox
        {
            Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };

        previewCard.Controls.Add(_pbPreview);
        previewCard.Controls.Add(lblPrev);

        rightPnl.Controls.Add(previewCard);
        rightPnl.Controls.Add(settingsCard);

        outer.Controls.Add(leftPnl,  0, 0);
        outer.Controls.Add(rightPnl, 1, 0);
        return outer;
    }

    // ════ تحميل البيانات ══════════════════════════════════════

    private void LoadData()
    {
        try
        {
            _allItems = _itemRepo.GetAll();
            _groups   = _itemRepo.GetGroups();

            _cmbGroup.Items.Clear();
            _cmbGroup.Items.Add(new IdItem(0, "— كل المجموعات —"));
            foreach (var g in _groups)
                _cmbGroup.Items.Add(new IdItem(g.Id, g.NameAr));
            _cmbGroup.SelectedIndex = 0;

            FilterItems();
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في تحميل الأصناف:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterItems()
    {
        var q   = _txtSearch.Text.Trim().ToLower();
        var gid = (_cmbGroup.SelectedItem as IdItem)?.Id ?? 0;

        var filtered = _allItems
            .Where(i =>
                (gid == 0 || i.GroupId == gid) &&
                (string.IsNullOrEmpty(q) ||
                 i.NameAr.ToLower().Contains(q)   ||
                 i.ItemCode.ToLower().Contains(q)  ||
                 i.Barcode.ToLower().Contains(q)))
            .ToList();

        _gridItems.Rows.Clear();
        foreach (var item in filtered)
        {
            int row = _gridItems.Rows.Add(
                item.ItemCode, item.NameAr,
                string.IsNullOrEmpty(item.Barcode) ? "— لا يوجد —" : item.Barcode,
                item.BarcodeType,
                item.RetailPrice.ToString("N2") + " ج.م",
                item.GroupName);
            _gridItems.Rows[row].Tag = item;

            if (string.IsNullOrEmpty(item.Barcode))
                _gridItems.Rows[row].DefaultCellStyle.ForeColor = AppTheme.MutedText;
        }
        _lblItemsStat.Text =
            $"إجمالي الأصناف: {filtered.Count}  " +
            $"|  بدون باركود: {filtered.Count(i => string.IsNullOrEmpty(i.Barcode))}  " +
            "  |  دبل كليك للإضافة الفردية";
    }

    // ════ إضافة إلى قائمة الطباعة ════════════════════════════

    private void OnAddSelected(object? s, EventArgs e)
    {
        var btype = _cmbBarcodeType.SelectedItem?.ToString() ?? "EAN-13";
        int added = 0;
        foreach (DataGridViewRow row in _gridItems.SelectedRows)
        {
            if (row.Tag is Item item)
            {
                AddToQueue(item, 1, btype);
                added++;
            }
        }
        if (added == 0)
            MessageBox.Show("اختر صنفاً أو أكثر أولاً.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            _tabs.SelectedTab = _tabPrint;
    }

    private void AddToQueue(Item item, int copies, string? btype = null)
    {
        // تجنب التكرار — إن موجود زوّد النسخ
        var existing = _printQueue.FirstOrDefault(l => l.ItemId == item.Id);
        if (existing != null)
        {
            existing.Copies += copies;
            RefreshPrintGrid();
            return;
        }

        var label = new BarcodeLabel
        {
            ItemId      = item.Id,
            ItemCode    = item.ItemCode,
            ItemName    = item.NameAr,
            Barcode     = string.IsNullOrEmpty(item.Barcode)
                            ? BarcodeService.GenerateEan13()
                            : item.Barcode,
            BarcodeType = btype ?? item.BarcodeType,
            RetailPrice = item.RetailPrice,
            GroupName   = item.GroupName,
            Copies      = copies
        };
        _printQueue.Add(label);
        RefreshPrintGrid();
        _tabs.SelectedTab = _tabPrint;
    }

    private void RefreshPrintGrid()
    {
        _gridPrint.Rows.Clear();
        foreach (var lbl in _printQueue)
        {
            int row = _gridPrint.Rows.Add(
                lbl.ItemName, lbl.Barcode, lbl.BarcodeType,
                lbl.PriceDisplay, lbl.Copies, "✖");
            _gridPrint.Rows[row].Tag = lbl;
        }
        UpdatePrintStat();
    }

    private void UpdatePrintStat()
    {
        int total = _printQueue.Sum(l => l.Copies);
        _lblPrintStat.Text =
            $"الأصناف: {_printQueue.Count}  |  إجمالي النسخ: {total}  " +
            $"|  الصفوف المطبوعة (تقريبي): {(int)Math.Ceiling(total / (double)_nudPerRow.Value)}";
    }

    private void OnCopiesChanged(object? s, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != _gridPrint.Columns["Copies"]!.Index || e.RowIndex < 0) return;
        if (_gridPrint.Rows[e.RowIndex].Tag is not BarcodeLabel lbl) return;
        if (int.TryParse(_gridPrint.Rows[e.RowIndex].Cells["Copies"].Value?.ToString(), out var c) && c > 0)
        {
            lbl.Copies = c;
            UpdatePrintStat();
        }
    }

    private void OnDeleteRow(object? s, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != _gridPrint.Columns["Del"]!.Index || e.RowIndex < 0) return;
        if (_gridPrint.Rows[e.RowIndex].Tag is BarcodeLabel lbl)
        {
            _printQueue.Remove(lbl);
            RefreshPrintGrid();
        }
    }

    // ════ معاينة الملصق ══════════════════════════════════════

    private void PreviewSelected()
    {
        if (_gridPrint.SelectedRows.Count == 0 || _printQueue.Count == 0)
        {
            if (_printQueue.Count > 0)
                PreviewLabel(_printQueue[0]);
            return;
        }
        if (_gridPrint.SelectedRows[0].Tag is BarcodeLabel lbl)
            PreviewLabel(lbl);
    }

    private void PreviewLabel(BarcodeLabel lbl)
    {
        try
        {
            var bmp = BarcodeService.DrawLabel(
                lbl,
                (int)_nudWidth.Value,
                (int)_nudHeight.Value,
                _chkShowCompany.Checked,
                _chkShowPrice.Checked,
                _chkShowName.Checked);

            // عرض مكبَّر في PictureBox
            _pbPreview.Image?.Dispose();
            _pbPreview.Image = bmp;
        }
        catch (Exception ex)
        {
            _lblPrintStat.Text = "خطأ في المعاينة: " + ex.Message;
        }
    }

    // ════ الطباعة ════════════════════════════════════════════

    private void OnPrintAll(object? s, EventArgs e)
    {
        if (_printQueue.Count == 0)
        {
            MessageBox.Show("قائمة الطباعة فارغة. أضف أصنافاً أولاً.",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int total = _printQueue.Sum(l => l.Copies);
        var confirm = MessageBox.Show(
            $"سيتم طباعة {total} ملصق ({_printQueue.Count} صنف).\nهل تريد المتابعة؟",
            "تأكيد الطباعة", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        var job = new LabelPrintJob
        {
            Labels       = _printQueue.ToList(),
            LabelsPerRow = (int)_nudPerRow.Value,
            LabelWidth   = (int)_nudWidth.Value,
            LabelHeight  = (int)_nudHeight.Value,
            ShowCompany  = _chkShowCompany.Checked,
            ShowItemName = _chkShowName.Checked,
            ShowPrice    = _chkShowPrice.Checked
        };

        try
        {
            PrintLabels(job);
        }
        catch (Exception ex)
        {
            MessageBox.Show("خطأ في الطباعة:\n" + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PrintLabels(LabelPrintJob job)
    {
        // نبني قائمة كل النسخ بالترتيب
        var allLabels = job.Labels
            .SelectMany(l => Enumerable.Repeat(l, l.Copies))
            .ToList();

        int perRow = job.LabelsPerRow;
        int lw     = job.LabelWidth;
        int lh     = job.LabelHeight;
        int margin = 8;

        var pd = new System.Drawing.Printing.PrintDocument();
        pd.DefaultPageSettings.Margins =
            new System.Drawing.Printing.Margins(20, 20, 20, 20);

        int currentIndex = 0;

        pd.PrintPage += (sender, ev) =>
        {
            if (ev.Graphics == null) return;
            var g       = ev.Graphics;
            var bounds  = ev.MarginBounds;
            int x0      = bounds.Left;
            int y0      = bounds.Top;
            int col     = 0;
            int rowY    = y0;

            while (currentIndex < allLabels.Count)
            {
                if (rowY + lh > bounds.Bottom)
                {
                    ev.HasMorePages = true;
                    return;
                }

                var lbl = allLabels[currentIndex];
                int x   = x0 + col * (lw + margin);

                if (x + lw > bounds.Right)
                {
                    col  = 0;
                    rowY += lh + margin;
                    if (rowY + lh > bounds.Bottom)
                    {
                        ev.HasMorePages = true;
                        return;
                    }
                    x = x0;
                }

                using var bmp = BarcodeService.DrawLabel(
                    lbl, lw, lh,
                    job.ShowCompany, job.ShowPrice, job.ShowItemName);
                g.DrawImage(bmp, x, rowY, lw, lh);

                col++;
                if (col >= perRow)
                {
                    col   = 0;
                    rowY += lh + margin;
                }
                currentIndex++;
            }
            ev.HasMorePages = false;
        };

        using var dlg = new System.Windows.Forms.PrintDialog { Document = pd };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            pd.Print();
    }

    // ── helper ────────────────────────────────────────────────
    private record IdItem(int Id, string Label) { public override string ToString() => Label; }
}
