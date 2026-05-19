using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للخزينة — TASK-022</summary>
internal class TreasuryRepository
{
    // ══ الخزائن ══════════════════════════════════════════════

    public List<Safe> GetSafes()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT id, COALESCE(name,''), COALESCE(name_ar,''),
                   COALESCE(balance,0), COALESCE(is_default,false), COALESCE(is_active,true)
            FROM public.safes
            WHERE is_active = true
            ORDER BY is_default DESC, name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<Safe>();
        while (r.Read())
            list.Add(new Safe
            {
                Id        = r.GetInt32(0),
                Name      = r.GetString(1),
                NameAr    = r.GetString(2),
                Balance   = r.GetDecimal(3),
                IsDefault = r.GetBoolean(4),
                IsActive  = r.GetBoolean(5)
            });
        return list;
    }

    public decimal GetSafeBalance(int safeId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT COALESCE(balance,0) FROM public.safes WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", safeId);
        return (decimal)(cmd.ExecuteScalar() ?? 0m);
    }

    // ══ بنود المصروفات ════════════════════════════════════════

    public List<ExpenseItem> GetExpenseItems()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT ei.id, COALESCE(ei.name,''), COALESCE(ei.name_ar,''),
                   COALESCE(eg.name_ar,'')
            FROM public.expense_items ei
            LEFT JOIN public.expense_groups eg ON eg.id = ei.group_id
            WHERE ei.is_active = true
            ORDER BY eg.name_ar, ei.name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<ExpenseItem>();
        while (r.Read())
            list.Add(new ExpenseItem
            {
                Id        = r.GetInt32(0),
                Name      = r.GetString(1),
                NameAr    = r.GetString(2),
                GroupName = r.GetString(3)
            });
        return list;
    }

    // ══ مصادر الإيرادات ══════════════════════════════════════

    public List<RevenueSource> GetRevenueSources()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT id, COALESCE(name,''), COALESCE(name_ar,'')
            FROM public.revenue_sources
            WHERE is_active = true
            ORDER BY name_ar
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        var list = new List<RevenueSource>();
        while (r.Read())
            list.Add(new RevenueSource
            {
                Id     = r.GetInt32(0),
                Name   = r.GetString(1),
                NameAr = r.GetString(2)
            });
        return list;
    }

    // ══ سندات الصرف ══════════════════════════════════════════

    public List<PaymentVoucher> GetPaymentVouchers(DateTime? from = null, DateTime? to = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT pv.id, pv.voucher_number, pv.safe_id, COALESCE(s.name_ar,''),
                   pv.amount, pv.expense_item_id, COALESCE(ei.name_ar,''),
                   COALESCE(pv.description,''), COALESCE(pv.reference_type,'manual'),
                   pv.reference_id, pv.voucher_date,
                   COALESCE(u.full_name,''), pv.created_at
            FROM public.payment_vouchers pv
            LEFT JOIN public.safes         s  ON s.id  = pv.safe_id
            LEFT JOIN public.expense_items ei ON ei.id = pv.expense_item_id
            LEFT JOIN auth.users           u  ON u.id  = pv.created_by
            WHERE 1=1
            """;
        if (from.HasValue) sql += " AND pv.voucher_date >= @from";
        if (to.HasValue)   sql += " AND pv.voucher_date <= @to";
        sql += " ORDER BY pv.voucher_date DESC, pv.id DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (from.HasValue) cmd.Parameters.AddWithValue("from", from.Value);
        if (to.HasValue)   cmd.Parameters.AddWithValue("to",   to.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<PaymentVoucher>();
        while (r.Read())
            list.Add(new PaymentVoucher
            {
                Id              = r.GetInt32(0),
                VoucherNumber   = r.GetString(1),
                SafeId          = r.GetInt32(2),
                SafeName        = r.GetString(3),
                Amount          = r.GetDecimal(4),
                ExpenseItemId   = r.IsDBNull(5) ? null : r.GetInt32(5),
                ExpenseItemName = r.GetString(6),
                Description     = r.GetString(7),
                ReferenceType   = r.GetString(8),
                ReferenceId     = r.IsDBNull(9) ? null : r.GetInt32(9),
                VoucherDate     = r.GetDateTime(10),
                CreatedBy       = r.GetString(11),
                CreatedAt       = r.GetDateTime(12)
            });
        return list;
    }

    public int CreatePaymentVoucher(int safeId, decimal amount, int? expenseItemId,
        string description, string refType, int? refId, int createdByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            var num = "SRF-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            const string ins = """
                INSERT INTO public.payment_vouchers
                    (voucher_number, safe_id, amount, expense_item_id,
                     description, reference_type, reference_id,
                     voucher_date, created_by)
                VALUES (@num,@sid,@amt,@exp,@desc,@rtype,@rid,@dt,@by)
                RETURNING id
                """;
            using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("num",   num);
            cmd.Parameters.AddWithValue("sid",   safeId);
            cmd.Parameters.AddWithValue("amt",   amount);
            cmd.Parameters.AddWithValue("exp",   (object?)expenseItemId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("desc",  (object?)description   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("rtype", refType);
            cmd.Parameters.AddWithValue("rid",   (object?)refId         ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dt",    DateTime.Today);
            cmd.Parameters.AddWithValue("by",    createdByUserId);
            int id = (int)cmd.ExecuteScalar()!;

            // خصم من رصيد الخزينة
            using var upd = new NpgsqlCommand(
                "UPDATE public.safes SET balance = balance - @amt WHERE id = @sid", conn, tx);
            upd.Parameters.AddWithValue("amt", amount);
            upd.Parameters.AddWithValue("sid", safeId);
            upd.ExecuteNonQuery();

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ سندات القبض ══════════════════════════════════════════

    public List<ReceiptVoucher> GetReceiptVouchers(DateTime? from = null, DateTime? to = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT rv.id, rv.voucher_number, rv.safe_id, COALESCE(s.name_ar,''),
                   rv.amount, rv.revenue_source_id, COALESCE(rs.name_ar,''),
                   COALESCE(rv.description,''), COALESCE(rv.reference_type,'manual'),
                   rv.reference_id, rv.voucher_date,
                   COALESCE(u.full_name,''), rv.created_at
            FROM public.receipt_vouchers rv
            LEFT JOIN public.safes           s  ON s.id  = rv.safe_id
            LEFT JOIN public.revenue_sources rs ON rs.id = rv.revenue_source_id
            LEFT JOIN auth.users             u  ON u.id  = rv.created_by
            WHERE 1=1
            """;
        if (from.HasValue) sql += " AND rv.voucher_date >= @from";
        if (to.HasValue)   sql += " AND rv.voucher_date <= @to";
        sql += " ORDER BY rv.voucher_date DESC, rv.id DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (from.HasValue) cmd.Parameters.AddWithValue("from", from.Value);
        if (to.HasValue)   cmd.Parameters.AddWithValue("to",   to.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<ReceiptVoucher>();
        while (r.Read())
            list.Add(new ReceiptVoucher
            {
                Id                = r.GetInt32(0),
                VoucherNumber     = r.GetString(1),
                SafeId            = r.GetInt32(2),
                SafeName          = r.GetString(3),
                Amount            = r.GetDecimal(4),
                RevenueSourceId   = r.IsDBNull(5) ? null : r.GetInt32(5),
                RevenueSourceName = r.GetString(6),
                Description       = r.GetString(7),
                ReferenceType     = r.GetString(8),
                ReferenceId       = r.IsDBNull(9) ? null : r.GetInt32(9),
                VoucherDate       = r.GetDateTime(10),
                CreatedBy         = r.GetString(11),
                CreatedAt         = r.GetDateTime(12)
            });
        return list;
    }

    public int CreateReceiptVoucher(int safeId, decimal amount, int? revenueSourceId,
        string description, string refType, int? refId, int createdByUserId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            var num = "QBD-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            const string ins = """
                INSERT INTO public.receipt_vouchers
                    (voucher_number, safe_id, amount, revenue_source_id,
                     description, reference_type, reference_id,
                     voucher_date, created_by)
                VALUES (@num,@sid,@amt,@src,@desc,@rtype,@rid,@dt,@by)
                RETURNING id
                """;
            using var cmd = new NpgsqlCommand(ins, conn, tx);
            cmd.Parameters.AddWithValue("num",   num);
            cmd.Parameters.AddWithValue("sid",   safeId);
            cmd.Parameters.AddWithValue("amt",   amount);
            cmd.Parameters.AddWithValue("src",   (object?)revenueSourceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("desc",  (object?)description      ?? DBNull.Value);
            cmd.Parameters.AddWithValue("rtype", refType);
            cmd.Parameters.AddWithValue("rid",   (object?)refId            ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dt",    DateTime.Today);
            cmd.Parameters.AddWithValue("by",    createdByUserId);
            int id = (int)cmd.ExecuteScalar()!;

            // إضافة لرصيد الخزينة
            using var upd = new NpgsqlCommand(
                "UPDATE public.safes SET balance = balance + @amt WHERE id = @sid", conn, tx);
            upd.Parameters.AddWithValue("amt", amount);
            upd.Parameters.AddWithValue("sid", safeId);
            upd.ExecuteNonQuery();

            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
    }
}
