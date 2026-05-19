using Npgsql;
using supermarket.Data;

namespace supermarket.Services;

/// <summary>
/// TASK-007 — سجل التدقيق: يسجل من فعل إيه ومتى
/// </summary>
internal static class AuditService
{
    /// <summary>
    /// يسجل حدثًا في audit.audit_log
    /// </summary>
    /// <param name="tableName">اسم الجدول المتأثر — مثل: auth.users, public.items</param>
    /// <param name="action">النوع: LOGIN | LOGOUT | CREATE | UPDATE | DELETE | APPROVE</param>
    /// <param name="recordId">معرف السجل المتأثر (اختياري)</param>
    /// <param name="oldValues">القيم القديمة JSON (اختياري)</param>
    /// <param name="newValues">القيم الجديدة JSON (اختياري)</param>
    public static void Log(string tableName,
                           string action,
                           int?   recordId  = null,
                           string? oldValues = null,
                           string? newValues = null)
    {
        try
        {
            int? userId = SessionContext.CurrentUser?.Id;

            using var conn = DatabaseConnection.CreateConnection();
            const string sql = """
                INSERT INTO audit.audit_log
                    (user_id, table_name, action, record_id, old_values, new_values, ip_address)
                VALUES
                    (@userId, @table, @action, @recordId, @old::jsonb, @new::jsonb, @ip)
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId",   userId.HasValue ? (object)userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("table",    tableName);
            cmd.Parameters.AddWithValue("action",   action);
            cmd.Parameters.AddWithValue("recordId", recordId.HasValue ? (object)recordId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("old",      oldValues  ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("new",      newValues  ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ip",       GetLocalIp());
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // سجل التدقيق لا يوقف التطبيق إذا فشل
        }
    }

    // Shortcut helpers
    public static void LogLogin(int userId)  => Log("auth.users", "LOGIN",  userId);
    public static void LogLogout(int userId) => Log("auth.users", "LOGOUT", userId);
    public static void LogCreate(string table, int recordId, string? newJson = null)
        => Log(table, "CREATE", recordId, null, newJson);
    public static void LogUpdate(string table, int recordId, string? oldJson = null, string? newJson = null)
        => Log(table, "UPDATE", recordId, oldJson, newJson);
    public static void LogDelete(string table, int recordId, string? oldJson = null)
        => Log(table, "DELETE", recordId, oldJson);
    public static void LogApprove(string table, int recordId)
        => Log(table, "APPROVE", recordId);

    private static string GetLocalIp()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { /* ignore */ }
        return "127.0.0.1";
    }
}
