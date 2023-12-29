using CSharp_TrayShortcut.Forms;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        private static void CatchException
            (object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                StreamWriter sw = new("crash-" + DateTime.Now.ToString().Replace("/", "-").Replace(":", "-") + ".txt");
                Exception ex = e.Exception;
                sw.WriteLine(ex.Message + ex.StackTrace);
                sw.Close();
            }
            finally
            {
                Application.Exit();
            }
        }

        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
            Application.ThreadException += new ThreadExceptionEventHandler(CatchException);
        }
    }
}
