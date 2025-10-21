using System.Collections.Generic;

namespace CSharp_TrayShortcut.Entities
{
    /// <summary>
    /// Entity used to save / load from config.json
    /// </summary>
    internal class Settings
    {
        /// <summary>
        /// List of custom shortcuts
        /// </summary>
        public List<CustomShortcuts> CustomShortcuts { get; set; } = [];

        /// <summary>
        /// Path of the folder you want to get shortcut from
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Path of folder icon
        /// </summary>
        public string PathFolderIcon { get; set; }

        /// <summary>
        /// Path of icon that will be in tray
        /// </summary>
        public string PathTrayIcon { get; set; }

        /// <summary>
        /// Force shot root files
        /// </summary>
        public bool? ShowRootFiles { get; set; }
    }
}
