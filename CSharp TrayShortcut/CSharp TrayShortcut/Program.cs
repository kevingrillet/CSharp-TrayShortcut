using CSharp_TrayShortcut.Forms;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        private static void CatchException
            (object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                using StreamWriter sw = new("crash-" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss") + ".txt");
                Exception ex = e.Exception;
                sw.WriteLine(ex.Message + ex.StackTrace);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += CatchException;
            Application.Run(new TrayApplicationContext());
        }
    }
}
