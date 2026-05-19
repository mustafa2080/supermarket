using System.Drawing;
using supermarket.Theme;

namespace supermarket.Views;

internal sealed class DashboardView : UserControl
{
    public event Action<string, string>? ModuleRequested;

    public DashboardView()
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        var cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Padding = new Padding(4),
            BackColor = AppTheme.Surface
        };

        Controls.Add(cardsPanel);

        AddModuleCard(cardsPanel, "الأصناف", "بطاقة صنف وإدارة البيانات الرئيسية للأصناف.", "بطاقة الصنف");
        AddModuleCard(cardsPanel, "المبيعات", "POS سريع يدعم البحث والباركود وتعليق الفواتير والدفع.", "المبيعات");
        AddModuleCard(cardsPanel, "المشتريات", "إدارة الموردين وفواتير الشراء والمرتجعات واعتماد المخزون.", "المشتريات");
        AddModuleCard(cardsPanel, "المخزون", "الأرصدة الحالية والجرد والتحويلات والتنبيهات.", "المخزون");
        AddModuleCard(cardsPanel, "الخزينة", "الورديات وسندات القبض والصرف ومراقبة الخزائن.", "الخزينة");
        AddModuleCard(cardsPanel, "الإعدادات", "البيانات الأساسية مثل الشركة والعملات والمستودعات.", "الإعدادات");
        AddModuleCard(cardsPanel, "التقارير", "تقارير المبيعات والمشتريات والمخزون والربحية.", "التقارير");
    }

    private void AddModuleCard(FlowLayoutPanel host, string title, string description, string actionTarget)
    {
        var card = AppTheme.CreateCard();
        card.Size = new Size(300, 150);

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Primary,
            Text = title,
            TextAlign = ContentAlignment.MiddleRight
        };

        var descriptionLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.BodyFont,
            ForeColor = AppTheme.MutedText,
            Text = description,
            TextAlign = ContentAlignment.TopRight
        };

        var actionButton = new Button
        {
            Dock = DockStyle.Bottom,
            Text = "فتح الوحدة",
            Height = 38
        };

        AppTheme.StylePrimaryButton(actionButton);
        actionButton.Click += (_, _) => ModuleRequested?.Invoke(actionTarget, description);

        card.Controls.Add(descriptionLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(actionButton);
        host.Controls.Add(card);
    }
}
