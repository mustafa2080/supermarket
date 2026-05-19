using System.Drawing;
using supermarket.Data.Repositories;
using supermarket.Models;
using supermarket.Services;
using supermarket.Theme;

namespace supermarket.Views;

/// <summary>
/// TASK-010 — شاشة إدارة المجموعات والأنواع (Tab مزدوج)
/// </summary>
internal sealed class CategoriesView : UserControl
{
    private readonly TabControl _tabs;

    public CategoriesView()
    {
        Dock        = DockStyle.Fill;
        BackColor   = AppTheme.Surface;
        RightToLeft = RightToLeft.Yes;

        _tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Font      = AppTheme.BodyFont,
            Appearance = TabAppearance.FlatButtons,
            ItemSize   = new Size(160, 34),
            SizeMode   = TabSizeMode.Fixed
        };

        _tabs.TabPages.Add(BuildGroupsTab());
        _tabs.TabPages.Add(BuildTypesTab());

        Controls.Add(_tabs);
    }

    // ──────────────────────────────────────────────────────────
    //  Tab المجموعات
    // ──────────────────────────────────────────────────────────
    private TabPage BuildGroupsTab()
    {
        var page = new TabPage("📂  مجموعات الأصناف") { BackColor = AppTheme.Surface };
        page.Controls.Add(new GroupsPanel());
        return page;
    }

    // ──────────────────────────────────────────────────────────
    //  Tab الأنواع
    // ──────────────────────────────────────────────────────────
    private TabPage BuildTypesTab()
    {
        var page = new TabPage("🏷️  أنواع الأصناف") { BackColor = AppTheme.Surface };
        page.Controls.Add(new TypesPanel());
        return page;
    }
}
