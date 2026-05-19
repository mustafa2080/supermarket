using Npgsql;

namespace supermarket.Data;

/// <summary>
/// إدارة الاتصال بقاعدة البيانات PostgreSQL
/// </summary>
internal static class DatabaseConnection
{
    // ─── Connection String ─────────────────────────────────────
    private static string _connectionString = BuildDefault();

    private static string BuildDefault() =>
        new NpgsqlConnectionStringBuilder
        {
            Host     = "localhost",
            Port     = 5432,
            Database = "supermarket",
            Username = "postgres",
            Password = "postgres",          // يُغيَّر من ملف الإعدادات
            Pooling  = true,
            MinPoolSize = 1,
            MaxPoolSize = 20,
            CommandTimeout = 30,
            Encoding = "UTF8"
        }.ToString();

    /// <summary>تحديث إعدادات الاتصال</summary>
    public static void Configure(string host, int port, string database,
                                 string username, string password)
    {
        _connectionString = new NpgsqlConnectionStringBuilder
        {
            Host     = host,
            Port     = port,
            Database = database,
            Username = username,
            Password = password,
            Pooling  = true,
            MinPoolSize = 1,
            MaxPoolSize = 20,
            CommandTimeout = 30,
            Encoding = "UTF8"
        }.ToString();
    }

    /// <summary>إنشاء اتصال جديد (يجب الـ Dispose بعد الاستخدام)</summary>
    public static NpgsqlConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>اختبار الاتصال بقاعدة البيانات</summary>
    public static bool TestConnection(out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            cmd.ExecuteScalar();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
