using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للعملاء — TASK-015</summary>
internal class CustomerRepository
{
    // ── الرقم التالي ─────────────────────────────────────────
    public string NextCustomerCode()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(MAX(CAST(SUBSTRING(code FROM 5) AS INTEGER)), 0) + 1
            FROM public.customers
            WHERE code LIKE 'CUS-%'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        int next = Convert.ToInt32(cmd.ExecuteScalar());
        return $"CUS-{next:D4}";
    }

    // ── قائمة العملاء ────────────────────────────────────────
    public List<Customer> GetAll(string? search = null, bool? activeOnly = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT id, COALESCE(code,''), name,
                   COALESCE(phone,''), COALESCE(email,''), COALESCE(address,''),
                   credit_limit, loyalty_points, is_active
            FROM public.customers
            WHERE (@search IS NULL OR name ILIKE @search OR phone ILIKE @search OR COALESCE(code,'') ILIKE @search)
              AND (@active IS NULL OR is_active = @active)
            ORDER BY name
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("search", search is null ? DBNull.Value : $"%{search}%");
        cmd.Parameters.AddWithValue("active", activeOnly.HasValue ? activeOnly.Value : DBNull.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<Customer>();
        while (r.Read()) list.Add(MapCustomer(r));
        return list;
    }

    // ── عميل واحد ────────────────────────────────────────────
    public Customer? GetById(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT id, COALESCE(code,''), name,
                   COALESCE(phone,''), COALESCE(email,''), COALESCE(address,''),
                   credit_limit, loyalty_points, is_active
            FROM public.customers WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? MapCustomer(r) : null;
    }

    // ── رصيد العميل (الآجل غير المسدد) ──────────────────────
    public decimal GetBalance(int customerId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(SUM(net_total - paid_amount), 0)
            FROM public.sales_invoices
            WHERE customer_id = @id AND payment_method = 'credit' AND status = 'completed'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", customerId);
        return (decimal)(cmd.ExecuteScalar() ?? 0m);
    }

    // ── حفظ عميل جديد ────────────────────────────────────────
    public int Insert(Customer c)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.customers
                (code, name, phone, email, address, credit_limit, loyalty_points, is_active)
            VALUES (@code, @name, @phone, @email, @address, @credit, @points, @active)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        AddParams(cmd, c);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    // ── تعديل عميل ───────────────────────────────────────────
    public void Update(Customer c)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.customers SET
                code=@code, name=@name, phone=@phone, email=@email,
                address=@address, credit_limit=@credit, is_active=@active
            WHERE id=@id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        AddParams(cmd, c);
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.ExecuteNonQuery();
    }

    // ── تفعيل / تعطيل ────────────────────────────────────────
    public void SetActive(int id, bool active)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE public.customers SET is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("a",  active);
        cmd.Parameters.AddWithValue("id", id);
        cmd.ExecuteNonQuery();
    }

    // ── تحقق تكرار الاسم ─────────────────────────────────────
    public bool NameExists(string name, int excludeId = 0)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM public.customers WHERE name=@n AND id<>@ex", conn);
        cmd.Parameters.AddWithValue("n",  name);
        cmd.Parameters.AddWithValue("ex", excludeId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ── كشف حساب العميل ──────────────────────────────────────
    public List<CustomerStatement> GetStatement(int customerId, DateTime from, DateTime to)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT invoice_number, invoice_date,
                   net_total, paid_amount, (net_total - paid_amount) AS remaining,
                   payment_method, status, COALESCE(notes,'')
            FROM public.sales_invoices
            WHERE customer_id = @id
              AND invoice_date >= @from
              AND invoice_date <= @to
            ORDER BY invoice_date DESC, id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id",   customerId);
        cmd.Parameters.AddWithValue("from", from);
        cmd.Parameters.AddWithValue("to",   to.AddDays(1));
        using var r = cmd.ExecuteReader();
        var list = new List<CustomerStatement>();
        while (r.Read())
            list.Add(new CustomerStatement
            {
                InvoiceNumber = r.GetString(0),
                InvoiceDate   = r.GetDateTime(1),
                NetTotal      = r.GetDecimal(2),
                PaidAmount    = r.GetDecimal(3),
                Remaining     = r.GetDecimal(4),
                PaymentMethod = r.GetString(5),
                Status        = r.GetString(6),
                Notes         = r.GetString(7)
            });
        return list;
    }

    // ── صرف نقاط الولاء ──────────────────────────────────────
    public bool RedeemPoints(int customerId, decimal points)
    {
        using var conn = DatabaseConnection.CreateConnection();
        // تحقق إن في نقاط كافية
        using var checkCmd = new NpgsqlCommand(
            "SELECT loyalty_points FROM public.customers WHERE id=@id", conn);
        checkCmd.Parameters.AddWithValue("id", customerId);
        decimal current = (decimal)(checkCmd.ExecuteScalar() ?? 0m);
        if (current < points || points < 100) return false;

        using var cmd = new NpgsqlCommand(
            "UPDATE public.customers SET loyalty_points = loyalty_points - @pts WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("pts", points);
        cmd.Parameters.AddWithValue("id",  customerId);
        cmd.ExecuteNonQuery();
        return true;
    }

    // ── helpers ───────────────────────────────────────────────
    private static void AddParams(NpgsqlCommand cmd, Customer c)
    {
        cmd.Parameters.AddWithValue("code",   string.IsNullOrEmpty(c.Code)  ? DBNull.Value : c.Code);
        cmd.Parameters.AddWithValue("name",   c.Name);
        cmd.Parameters.AddWithValue("phone",  string.IsNullOrEmpty(c.Phone)   ? DBNull.Value : c.Phone);
        cmd.Parameters.AddWithValue("email",  string.IsNullOrEmpty(c.Email)   ? DBNull.Value : c.Email);
        cmd.Parameters.AddWithValue("address",string.IsNullOrEmpty(c.Address) ? DBNull.Value : c.Address);
        cmd.Parameters.AddWithValue("credit", c.CreditLimit);
        cmd.Parameters.AddWithValue("points", c.LoyaltyPoints);
        cmd.Parameters.AddWithValue("active", c.IsActive);
    }

    private static Customer MapCustomer(NpgsqlDataReader r) => new()
    {
        Id            = r.GetInt32(0),
        Code          = r.GetString(1),
        Name          = r.GetString(2),
        Phone         = r.GetString(3),
        Email         = r.GetString(4),
        Address       = r.GetString(5),
        CreditLimit   = r.GetDecimal(6),
        LoyaltyPoints = r.GetDecimal(7),
        IsActive      = r.GetBoolean(8)
    };
}
