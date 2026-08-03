using System;

namespace Switch
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                SelfTests.Run();
                Console.WriteLine("Switch conversion tests passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                return 1;
            }
        }
    }
}
