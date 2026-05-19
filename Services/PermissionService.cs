namespace supermarket.Services;

/// <summary>
/// TASK-006 — فحص الصلاحيات قبل كل عملية
/// </summary>
internal static class PermissionService
{
    /// <summary>هل المستخدم الحالي يملك الصلاحية المطلوبة؟</summary>
    /// <param name="module">اسم الوحدة — مثل: items, purchases, sales, users, treasury, warehouse, reports, settings</param>
    /// <param name="action">الإجراء — مثل: view, create, edit, delete, approve, print</param>
    public static bool HasPermission(string module, string action)
    {
        if (!SessionContext.IsLoggedIn) return false;
        if (SessionContext.IsAdmin)    return true;   // Admin يملك كل شيء
        return SessionContext.Permissions.Contains($"{module}.{action}");
    }

    /// <summary>يُخفي أو يُعطّل زرًا بناءً على الصلاحية</summary>
    public static void ApplyToButton(System.Windows.Forms.Button btn,
                                     string module, string action)
    {
        bool allowed = HasPermission(module, action);
        btn.Enabled = allowed;
        btn.Visible = allowed;
    }

    /// <summary>يرمي استثناء إذا لم تكن الصلاحية موجودة</summary>
    public static void Require(string module, string action)
    {
        if (!HasPermission(module, action))
            throw new UnauthorizedAccessException(
                $"ليس لديك صلاحية '{action}' في وحدة '{module}'.");
    }

    // Shortcuts شائعة
    public static bool CanView(string module)   => HasPermission(module, "view");
    public static bool CanCreate(string module) => HasPermission(module, "create");
    public static bool CanEdit(string module)   => HasPermission(module, "edit");
    public static bool CanDelete(string module) => HasPermission(module, "delete");
    public static bool CanApprove(string module)=> HasPermission(module, "approve");
    public static bool CanPrint(string module)  => HasPermission(module, "print");
}
