using System;
using System.Linq;
using System.Windows.Forms;

namespace Switch
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Any(argument => string.Equals(argument, "--test", StringComparison.OrdinalIgnoreCase)))
            {
                SelfTests.Run();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ShowStartupMessage();
            Application.Run(new HotkeyWindow());
        }

        private static void ShowStartupMessage()
        {
            const string message = "تم تشغيل برنامج Switch بنجاح وهو يعمل الآن في الخلفية.\n\n" +
                "طريقة الاستخدام:\n\n" +
                "1. حدد النص المكتوب بتخطيط خاطئ.\n" +
                "2. اضغط Ctrl + Shift + Space لتحويله فوراً.\n" +
                "للخروج من البرنامج، انقر بزر الفأرة الأيمن على الأيقونة بجانب الساعة واختر خروج.\n\n" +
                "حقوق النشر محفوظة لـ ahmedjamalzaki@ ©"; ;

            MessageBox.Show(message, "Switch - EN ↔ AR", MessageBoxButtons.OK, MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        }
    }
}
