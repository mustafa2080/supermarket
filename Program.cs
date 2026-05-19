using supermarket.Views;

namespace supermarket;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // فتح شاشة تسجيل الدخول أولاً
        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK)
        {
            // المستخدم أغلق الشاشة بدون تسجيل دخول
            return;
        }

        // تسجيل الدخول نجح — افتح الشاشة الرئيسية
        Application.Run(new MainForm());
    }
}
