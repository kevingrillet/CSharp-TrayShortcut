using CSharp_TrayShortcut.Forms;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        private const string _singleInstanceMutexName = "CSharp_TrayShortcut_SingleInstance";
        private static Mutex _singleInstanceMutex;

        /// <summary>
        /// Write a crash report to a uniquely named file next to the executable, then exit.
        /// Logging failures are swallowed so they can never mask the original crash.
        /// </summary>
        private static void WriteCrashLog(Exception ex)
        {
            try
            {
                string fileName = "crash-" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss")
                    + "-" + Guid.NewGuid().ToString("N") + ".txt";
                string path = Path.Combine(AppContext.BaseDirectory, fileName);
                using StreamWriter sw = new(path);
                sw.WriteLine((ex?.Message ?? "Unknown error") + Environment.NewLine + ex?.StackTrace);
            }
            catch
            {
                // Nothing more we can do if even logging fails.
            }
        }

        private static void CatchException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                WriteCrashLog(e.Exception);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                WriteCrashLog(e.ExceptionObject as Exception);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        [STAThread]
        private static void Main()
        {
            // Allow only one running instance (otherwise we get duplicate tray icons).
            _singleInstanceMutex = new Mutex(true, _singleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                return;
            }

            try
            {
                ApplicationConfiguration.Initialize();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += CatchException;
                AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;
                Application.Run(new TrayApplicationContext());
            }
            finally
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
        }
    }
}
