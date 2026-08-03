using System;
using System.Linq;
using System.Threading;
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

            bool createdNewInstance;
            using (var singleInstance = new Mutex(true, @"Local\Switch.KeyboardLayoutConverter", out createdNewInstance))
            {
                if (!createdNewInstance) return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new HotkeyWindow());
            }
        }
    }
}
