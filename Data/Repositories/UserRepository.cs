using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>
/// Repository للمستخدمين — تسجيل الدخول، الإدارة، الصلاحيات
/// </summary>
internal class UserRepository
{
    // ── تسجيل الدخول ─────────────────────────────────────────
    public User? GetByUsername(string username)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT u.id, u.username, u.password_hash, u.full_name,
                   u.role_id, r.name AS role_name, r.name_ar AS role_name_ar,
                   u.is_active, u.last_login, u.login_attempts, u.locked_until
            FROM auth.users u
            JOIN auth.roles r ON u.role_id = r.id
            WHERE u.username = @username
            """;

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return MapUser(reader);
    }

    public void UpdateLastLogin(int userId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE auth.users
            SET last_login = NOW(), login_attempts = 0, locked_until = NULL
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", userId);
        cmd.ExecuteNonQuery();
    }

    public void IncrementLoginAttempts(int userId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE auth.users
            SET login_attempts = login_attempts + 1,
                locked_until = CASE
                    WHEN login_attempts + 1 >= 5 THEN NOW() + INTERVAL '15 minutes'
                    ELSE locked_until
                END
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", userId);
        cmd.ExecuteNonQuery();
    }

    // ── CRUD المستخدمين ───────────────────────────────────────
    public List<User> GetAll()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT u.id, u.username, u.password_hash, u.full_name,
                   u.role_id, r.name AS role_name, r.name_ar AS role_name_ar,
                   u.is_active, u.last_login, u.login_attempts, u.locked_until
            FROM auth.users u
            JOIN auth.roles r ON u.role_id = r.id
            ORDER BY u.full_name
            """;
        using var cmd    = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var list = new List<User>();
        while (reader.Read()) list.Add(MapUser(reader));
        return list;
    }

    public int Insert(User user, string passwordHash)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO auth.users (username, password_hash, full_name, role_id, is_active)
            VALUES (@username, @hash, @fullName, @roleId, @active)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("username", user.Username);
        cmd.Parameters.AddWithValue("hash",     passwordHash);
        cmd.Parameters.AddWithValue("fullName", user.FullName);
        cmd.Parameters.AddWithValue("roleId",   user.RoleId);
        cmd.Parameters.AddWithValue("active",   user.IsActive);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void Update(User user)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE auth.users
            SET full_name = @fullName, role_id = @roleId,
                is_active = @active, updated_at = NOW()
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("fullName", user.FullName);
        cmd.Parameters.AddWithValue("roleId",   user.RoleId);
        cmd.Parameters.AddWithValue("active",   user.IsActive);
        cmd.Parameters.AddWithValue("id",       user.Id);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePassword(int userId, string newHash)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE auth.users
            SET password_hash = @hash, updated_at = NOW()
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("hash", newHash);
        cmd.Parameters.AddWithValue("id",   userId);
        cmd.ExecuteNonQuery();
    }

    // ── الصلاحيات ─────────────────────────────────────────────
    public HashSet<string> GetPermissions(int roleId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT p.module || '.' || p.action
            FROM auth.role_permissions rp
            JOIN auth.permissions p ON rp.permission_id = p.id
            WHERE rp.role_id = @roleId
            """;
        using var cmd    = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("roleId", roleId);
        using var reader = cmd.ExecuteReader();
        var set = new HashSet<string>();
        while (reader.Read()) set.Add(reader.GetString(0));
        return set;
    }

    // ── Mapping ───────────────────────────────────────────────
    private static User MapUser(NpgsqlDataReader r) => new()
    {
        Id            = r.GetInt32(r.GetOrdinal("id")),
        Username      = r.GetString(r.GetOrdinal("username")),
        PasswordHash  = r.GetString(r.GetOrdinal("password_hash")),
        FullName      = r.GetString(r.GetOrdinal("full_name")),
        RoleId        = r.GetInt32(r.GetOrdinal("role_id")),
        RoleName      = r.GetString(r.GetOrdinal("role_name")),
        RoleNameAr    = r.GetString(r.GetOrdinal("role_name_ar")),
        IsActive      = r.GetBoolean(r.GetOrdinal("is_active")),
        LoginAttempts = r.GetInt32(r.GetOrdinal("login_attempts")),
        LastLogin     = r.IsDBNull(r.GetOrdinal("last_login"))   ? null : r.GetDateTime(r.GetOrdinal("last_login")),
        LockedUntil   = r.IsDBNull(r.GetOrdinal("locked_until")) ? null : r.GetDateTime(r.GetOrdinal("locked_until"))
    };
}
