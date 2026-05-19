using Npgsql;
using supermarket.Models;

namespace supermarket.Data.Repositories;

/// <summary>Repository للورديات — TASK-023</summary>
internal class ShiftRepository
{
    // ══ الوردية الحالية ══════════════════════════════════════

    /// <summary>الوردية المفتوحة حالياً (أي مستخدم)</summary>
    public TreasuryShift? GetOpenShift()
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT ts.id, ts.shift_number, ts.cashier_id, COALESCE(u.full_name,''),
                   ts.safe_id, COALESCE(s.name,''),
                   ts.opening_balance, COALESCE(ts.expected_closing,0),
                   ts.actual_closing, ts.difference,
                   ts.opened_at, ts.closed_at, ts.status, COALESCE(ts.notes,'')
            FROM public.treasury_shifts ts
            LEFT JOIN auth.users    u ON u.id = ts.cashier_id
            LEFT JOIN public.safes  s ON s.id = ts.safe_id
            WHERE ts.status = 'open'
            ORDER BY ts.opened_at DESC
            LIMIT 1
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return MapShift(r);
    }

    /// <summary>الوردية المفتوحة للكاشير الحالي</summary>
    public TreasuryShift? GetOpenShiftForUser(int userId)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT ts.id, ts.shift_number, ts.cashier_id, COALESCE(u.full_name,''),
                   ts.safe_id, COALESCE(s.name,''),
                   ts.opening_balance, COALESCE(ts.expected_closing,0),
                   ts.actual_closing, ts.difference,
                   ts.opened_at, ts.closed_at, ts.status, COALESCE(ts.notes,'')
            FROM public.treasury_shifts ts
            LEFT JOIN auth.users    u ON u.id = ts.cashier_id
            LEFT JOIN public.safes  s ON s.id = ts.safe_id
            WHERE ts.status = 'open' AND ts.cashier_id = @uid
            ORDER BY ts.opened_at DESC
            LIMIT 1
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("uid", userId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return MapShift(r);
    }

    // ══ قائمة الورديات ═══════════════════════════════════════

    public List<TreasuryShift> GetAll(int? userId = null)
    {
        using var conn = DatabaseConnection.CreateConnection();
        var sql = """
            SELECT ts.id, ts.shift_number, ts.cashier_id, COALESCE(u.full_name,''),
                   ts.safe_id, COALESCE(s.name,''),
                   ts.opening_balance, COALESCE(ts.expected_closing,0),
                   ts.actual_closing, ts.difference,
                   ts.opened_at, ts.closed_at, ts.status, COALESCE(ts.notes,'')
            FROM public.treasury_shifts ts
            LEFT JOIN auth.users    u ON u.id = ts.cashier_id
            LEFT JOIN public.safes  s ON s.id = ts.safe_id
            WHERE 1=1
            """;
        if (userId.HasValue) sql += " AND ts.cashier_id = @uid";
        sql += " ORDER BY ts.opened_at DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        if (userId.HasValue) cmd.Parameters.AddWithValue("uid", userId.Value);
        using var r = cmd.ExecuteReader();
        var list = new List<TreasuryShift>();
        while (r.Read()) list.Add(MapShift(r));
        return list;
    }

    // ══ فتح وردية ════════════════════════════════════════════

    public TreasuryShift OpenShift(int cashierId, int? safeId, decimal openingBalance, string notes)
    {
        using var conn = DatabaseConnection.CreateConnection();

        // تأكد مفيش وردية مفتوحة لنفس الكاشير
        using (var chk = new NpgsqlCommand(
            "SELECT COUNT(*) FROM public.treasury_shifts WHERE cashier_id=@uid AND status='open'", conn))
        {
            chk.Parameters.AddWithValue("uid", cashierId);
            if ((long)chk.ExecuteScalar()! > 0)
                throw new InvalidOperationException("يوجد وردية مفتوحة بالفعل — أغلقها أولاً.");
        }

        var num = "SHF-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        const string ins = """
            INSERT INTO public.treasury_shifts
                (shift_number, cashier_id, safe_id, opening_balance, opened_at, status, notes)
            VALUES (@num, @uid, @sid, @ob, NOW(), 'open', @notes)
            RETURNING id
            """;
        using var cmd = new NpgsqlCommand(ins, conn);
        cmd.Parameters.AddWithValue("num",   num);
        cmd.Parameters.AddWithValue("uid",   cashierId);
        cmd.Parameters.AddWithValue("sid",   (object?)safeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ob",    openingBalance);
        cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
        int id = (int)cmd.ExecuteScalar()!;

        return GetById(id)!;
    }

    // ══ ملخص الوردية ═════════════════════════════════════════

    public ShiftSummary GetShiftSummary(int shiftId)
    {
        using var conn = DatabaseConnection.CreateConnection();

        decimal opening = 0, sales = 0, returns = 0, expenses = 0;

        // رصيد الفتح
        using (var cmd = new NpgsqlCommand(
            "SELECT opening_balance FROM public.treasury_shifts WHERE id=@id", conn))
        {
            cmd.Parameters.AddWithValue("id", shiftId);
            opening = (decimal)(cmd.ExecuteScalar() ?? 0m);
        }

        // إجمالي المبيعات النقدية في هذه الوردية
        using (var cmd = new NpgsqlCommand("""
            SELECT COALESCE(SUM(net_total),0)
            FROM public.sales_invoices
            WHERE shift_id=@id AND status='completed' AND payment_method='cash'
            """, conn))
        {
            cmd.Parameters.AddWithValue("id", shiftId);
            sales = (decimal)cmd.ExecuteScalar()!;
        }

        // إجمالي المرتجعات النقدية
        using (var cmd = new NpgsqlCommand("""
            SELECT COALESCE(SUM(sr.total_amount),0)
            FROM public.sales_returns sr
            JOIN public.sales_invoices si ON si.id = sr.original_invoice_id
            WHERE si.shift_id=@id AND sr.refund_method='cash' AND sr.status='completed'
            """, conn))
        {
            cmd.Parameters.AddWithValue("id", shiftId);
            returns = (decimal)cmd.ExecuteScalar()!;
        }

        // إجمالي سندات الصرف في هذه الوردية (مصروفات نقدية)
        using (var cmd = new NpgsqlCommand("""
            SELECT COALESCE(SUM(pv.amount),0)
            FROM public.payment_vouchers pv
            JOIN public.treasury_shifts ts ON ts.safe_id = pv.safe_id
            WHERE ts.id=@id
              AND pv.voucher_date::date = ts.opened_at::date
              AND pv.reference_type = 'manual'
            """, conn))
        {
            cmd.Parameters.AddWithValue("id", shiftId);
            expenses = (decimal)cmd.ExecuteScalar()!;
        }

        return new ShiftSummary
        {
            ShiftId        = shiftId,
            OpeningBalance = opening,
            TotalSales     = sales,
            TotalReturns   = returns,
            TotalExpenses  = expenses
        };
    }

    // ══ إغلاق الوردية ════════════════════════════════════════

    public void CloseShift(int shiftId, decimal actualClosing, string notes)
    {
        using var conn = DatabaseConnection.CreateConnection();
        using var tx   = conn.BeginTransaction();
        try
        {
            var summary = GetShiftSummary(shiftId);
            decimal diff = actualClosing - summary.ExpectedClosing;

            const string upd = """
                UPDATE public.treasury_shifts
                SET status='closed', actual_closing=@ac, expected_closing=@ec,
                    difference=@diff, closed_at=NOW(), notes=@notes
                WHERE id=@id
                """;
            using var cmd = new NpgsqlCommand(upd, conn, tx);
            cmd.Parameters.AddWithValue("ac",    actualClosing);
            cmd.Parameters.AddWithValue("ec",    summary.ExpectedClosing);
            cmd.Parameters.AddWithValue("diff",  diff);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id",    shiftId);
            cmd.ExecuteNonQuery();

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ══ مساعدات ══════════════════════════════════════════════

    public TreasuryShift? GetById(int id)
    {
        using var conn = DatabaseConnection.CreateConnection();
        const string sql = """
            SELECT ts.id, ts.shift_number, ts.cashier_id, COALESCE(u.full_name,''),
                   ts.safe_id, COALESCE(s.name,''),
                   ts.opening_balance, COALESCE(ts.expected_closing,0),
                   ts.actual_closing, ts.difference,
                   ts.opened_at, ts.closed_at, ts.status, COALESCE(ts.notes,'')
            FROM public.treasury_shifts ts
            LEFT JOIN auth.users    u ON u.id = ts.cashier_id
            LEFT JOIN public.safes  s ON s.id = ts.safe_id
            WHERE ts.id = @id
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return MapShift(r);
    }

    private static TreasuryShift MapShift(NpgsqlDataReader r) => new()
    {
        Id              = r.GetInt32(0),
        ShiftNumber     = r.GetString(1),
        CashierId       = r.GetInt32(2),
        CashierName     = r.GetString(3),
        SafeId          = r.IsDBNull(4) ? null : r.GetInt32(4),
        SafeName        = r.GetString(5),
        OpeningBalance  = r.GetDecimal(6),
        ExpectedClosing = r.GetDecimal(7),
        ActualClosing   = r.IsDBNull(8) ? null : r.GetDecimal(8),
        Difference      = r.IsDBNull(9) ? null : r.GetDecimal(9),
        OpenedAt        = r.GetDateTime(10),
        ClosedAt        = r.IsDBNull(11) ? null : r.GetDateTime(11),
        Status          = r.GetString(12),
        Notes           = r.GetString(13)
    };
}
