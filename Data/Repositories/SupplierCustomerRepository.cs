using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للموردين والعملاء</summary>
internal class SupplierRepository
{
    // ── رصيد المورد (إجمالي المستحق) ───────────────────────
    public decimal GetBalance(int supplierId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT COALESCE(SUM(remaining), 0)
            FROM public.purchase_invoices
            WHERE supplier_id = @id AND status = 'approved'
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", supplierId);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    // ── كشف حساب المورد ─────────────────────────────────────
    public List<SupplierStatement> GetStatement(int supplierId, DateTime? from = null, DateTime? to = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT invoice_number, invoice_date, net_total, paid_amount, remaining,
                   payment_method, status, notes
            FROM public.purchase_invoices
            WHERE supplier_id = @id
              AND (@from IS NULL OR invoice_date >= @from)
              AND (@to   IS NULL OR invoice_date <= @to)
            ORDER BY invoice_date DESC, id DESC
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id",   supplierId);
        cmd.Parameters.AddWithValue("from", (object?)from ?? DBNull.Value);
        cmd.Parameters.AddWithValue("to",   (object?)to   ?? DBNull.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<SupplierStatement>();
        while (r.Read())
            list.Add(new SupplierStatement
            {
                InvoiceNumber = r.GetString(0),
                InvoiceDate   = r.GetDateTime(1),
                NetTotal      = r.GetDecimal(2),
                PaidAmount    = r.GetDecimal(3),
                Remaining     = r.GetDecimal(4),
                PaymentMethod = r.GetString(5),
                Status        = r.GetString(6),
                Notes         = r.IsDBNull(7) ? "" : r.GetString(7)
            });
        return list;
    }

    // ── تحقق من تكرار الاسم ─────────────────────────────────
    public bool NameExists(string name, int excludeId = 0)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM public.suppliers WHERE name = @name AND id <> @ex";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("ex",   excludeId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ── تفعيل / تعطيل ───────────────────────────────────────
    public void SetActive(int id, bool active)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd  = new NpgsqlCommand(
            "UPDATE public.suppliers SET is_active = @active WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("active", active);
        cmd.Parameters.AddWithValue("id",     id);
        cmd.ExecuteNonQuery();
    }

    public List<Supplier> GetAll(bool activeOnly = true)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = "SELECT id, COALESCE(code,''), name, COALESCE(phone,''), COALESCE(mobile,''), " +
                  "COALESCE(email,''), COALESCE(address,''), COALESCE(tax_number,''), " +
                  "credit_limit, COALESCE(notes,''), is_active FROM public.suppliers" +
                  (activeOnly ? " WHERE is_active = TRUE" : "") + " ORDER BY name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Supplier>();
        while (r.Read()) list.Add(MapSupplier(r));
        return list;
    }

    public int Insert(Supplier s)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.suppliers
                (code, name, phone, mobile, email, address, tax_number, credit_limit, notes)
            VALUES (@code, @name, @phone, @mobile, @email, @address, @tax, @credit, @notes)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code",   (object?)s.Code        ?? DBNull.Value);
        cmd.Parameters.AddWithValue("name",   s.Name);
        cmd.Parameters.AddWithValue("phone",  (object?)s.Phone       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mobile", (object?)s.Mobile      ?? DBNull.Value);
        cmd.Parameters.AddWithValue("email",  (object?)s.Email       ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address",(object?)s.Address     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("tax",    (object?)s.TaxNumber   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("credit", s.CreditLimit);
        cmd.Parameters.AddWithValue("notes",  (object?)s.Notes       ?? DBNull.Value);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void Update(Supplier s)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            UPDATE public.suppliers SET
                name = @name, phone = @phone, mobile = @mobile,
                email = @email, address = @address, tax_number = @tax,
                credit_limit = @credit, notes = @notes, is_active = @active
            WHERE id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",   s.Name);
        cmd.Parameters.AddWithValue("phone",  (object?)s.Phone     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mobile", (object?)s.Mobile    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("email",  (object?)s.Email     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address",(object?)s.Address   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("tax",    (object?)s.TaxNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("credit", s.CreditLimit);
        cmd.Parameters.AddWithValue("notes",  (object?)s.Notes     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("active", s.IsActive);
        cmd.Parameters.AddWithValue("id",     s.Id);
        cmd.ExecuteNonQuery();
    }

    private static Supplier MapSupplier(NpgsqlDataReader r) => new()
    {
        Id          = r.GetInt32(0),
        Code        = r.GetString(1),
        Name        = r.GetString(2),
        Phone       = r.GetString(3),
        Mobile      = r.GetString(4),
        Email       = r.GetString(5),
        Address     = r.GetString(6),
        TaxNumber   = r.GetString(7),
        CreditLimit = r.GetDecimal(8),
        Notes       = r.GetString(9),
        IsActive    = r.GetBoolean(10)
    };
}

internal class CustomerRepository
{
    public List<Customer> GetAll(bool activeOnly = true)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = "SELECT id, COALESCE(code,''), name, COALESCE(phone,''), COALESCE(email,''), " +
                  "COALESCE(address,''), credit_limit, loyalty_points, is_active " +
                  "FROM public.customers" +
                  (activeOnly ? " WHERE is_active = TRUE" : "") + " ORDER BY name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Customer>();
        while (r.Read()) list.Add(new Customer
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
        });
        return list;
    }

    public int Insert(Customer c)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            INSERT INTO public.customers (code, name, phone, email, address, credit_limit)
            VALUES (@code, @name, @phone, @email, @address, @credit)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code",   (object?)c.Code    ?? DBNull.Value);
        cmd.Parameters.AddWithValue("name",   c.Name);
        cmd.Parameters.AddWithValue("phone",  (object?)c.Phone   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("email",  (object?)c.Email   ?? DBNull.Value);
        cmd.Parameters.AddWithValue("address",(object?)c.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("credit", c.CreditLimit);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }
}
