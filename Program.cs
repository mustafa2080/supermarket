using supermarket.Data;
using supermarket.Views;

namespace supermarket;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ── تحقق من الاتصال بقاعدة البيانات أولاً ──────────────
        if (!DatabaseConnection.TestConnection(out string dbError))
        {
            MessageBox.Show(
                $"❌ تعذّر الاتصال بقاعدة البيانات:\n\n{dbError}\n\n" +
                "تأكد من:\n" +
                "• تشغيل PostgreSQL\n" +
                "• صحة كلمة المرور في DatabaseConnection.cs\n" +
                "• وجود قاعدة بيانات اسمها 'supermarket'",
                "خطأ في الاتصال",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // فتح شاشة تسجيل الدخول
        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK)
            return;

        Application.Run(new MainForm());
    }
}
