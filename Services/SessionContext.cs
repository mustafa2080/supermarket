using supermarket.Models;

namespace supermarket.Services;

/// <summary>
/// بيانات الجلسة الحالية — مستخدم واحد طول عمر التطبيق
/// </summary>
internal static class SessionContext
{
    public static User?           CurrentUser      { get; private set; }
    public static HashSet<string> Permissions      { get; private set; } = new();
    public static bool            IsLoggedIn       => CurrentUser is not null;
    public static string          DisplayName      => CurrentUser?.FullName ?? "غير معرف";
    public static string          RoleAr           => CurrentUser?.RoleNameAr ?? "";
    public static DateTime?       SessionStartedAt { get; private set; }

    public static void StartSession(User user, HashSet<string> permissions)
    {
        CurrentUser      = user;
        Permissions      = permissions;
        SessionStartedAt = DateTime.Now;
    }

    public static void EndSession()
    {
        CurrentUser      = null;
        Permissions      = new();
        SessionStartedAt = null;
    }

    /// <summary>هل المستخدم الحالي Admin؟</summary>
    public static bool IsAdmin => CurrentUser?.RoleName == "admin";
}
