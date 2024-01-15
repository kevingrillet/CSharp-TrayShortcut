using System.Windows.Forms;

namespace CSharp_TrayShortcut.Forms
{
    /// <summary>
    /// ToolStripMenuItem + Path & Argument for Process.Start
    /// </summary>
    internal class CustomToolStripMenuItem : ToolStripMenuItem
    {
        /// <summary>
        /// Property used if there is a need of argument
        /// </summary>
        public string Argument { get; set; }

        /// <summary>
        /// Property used in Process.Start
        /// </summary>
        public string Path { get; set; }
    }
}
