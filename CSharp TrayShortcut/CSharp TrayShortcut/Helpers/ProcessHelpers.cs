using CSharp_TrayShortcut.Forms;
using System.Diagnostics;

namespace CSharp_TrayShortcut.Helpers
{
    /// <summary>
    /// Help run Processes.
    /// </summary>
    internal static class ProcessHelpers
    {
        /// <summary>
        /// Event to run from CustomToolStripMenuItem.Path & Argument
        /// </summary>
        /// <param name="sender">CustomToolStripMenuItem</param>
        /// <param name="e">Unused</param>
        /// <exception cref="ApplicationException">If sender is not of CustomToolStripMenuItem, or if sender.Path is null/empty</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S112:General exceptions should never be thrown", Justification = "<En attente>")]
        public static void Run(object sender, EventArgs e)
        {
            if (sender is not CustomToolStripMenuItem)
            {
                throw new ApplicationException($"Sender is not of the expected type, should be {nameof(CustomToolStripMenuItem)}, but is: {sender.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace((sender as CustomToolStripMenuItem).Path))
            {
                throw new ApplicationException("Path is required.");
            }

            if (string.IsNullOrWhiteSpace((sender as CustomToolStripMenuItem).Argument))
            {
                Process.Start(new ProcessStartInfo((sender as CustomToolStripMenuItem).Path)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo((sender as CustomToolStripMenuItem).Path)
                {
                    Arguments = (sender as CustomToolStripMenuItem).Argument,
                    UseShellExecute = true
                });
            }
        }
    }
}
