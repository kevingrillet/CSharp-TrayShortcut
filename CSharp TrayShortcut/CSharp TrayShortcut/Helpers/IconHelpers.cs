using Shell32;

namespace CSharp_TrayShortcut.Helpers
{
    /// <summary>
    /// Help manage Icons
    /// </summary>
    internal static class IconHelpers
    {
        /// <summary>
        /// File path to bitmap
        /// </summary>
        /// <param name="filePath">Any kind of file</param>
        /// <returns>Bitmap of ICO extracted</returns>
        public static Bitmap ExtractIconToBitmap(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return null;
            }
            if (Path.GetExtension(filePath) == ".lnk")
            {
                var target = GetShortcutTargetFile(filePath);
                if (!string.IsNullOrWhiteSpace(target) && File.Exists(target))
                {
                    return Icon.ExtractAssociatedIcon(target)?.ToBitmap();
                }
            }
            return Icon.ExtractAssociatedIcon(filePath)?.ToBitmap();
        }

        /// <summary>
        /// ICO path to bitmap
        /// </summary>
        /// <param name="path">ICO path</param>
        /// <returns>Bitmap of ICO</returns>
        public static Bitmap IconPathToBitmap(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            return SetIcon(path)?.ToBitmap();
        }

        /// <summary>
        /// ICO from path
        /// </summary>
        /// <param name="path">Path of an ICO</param>
        /// <returns>Ico</returns>
        public static Icon SetIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            if (File.Exists(path))
            {
                return new Icon(path);
            }
            else if (File.Exists(Path.Combine("Ressources", path)))
            {
                return new Icon(Path.Combine("Ressources", path));
            }
            return null;
        }

        /// <summary>
        /// Get file path from shortcut
        /// </summary>
        /// <param name="shortcutFilename">Shortcut file</param>
        /// <returns>File path of shortcut</returns>
        /// <remarks>Source: https://stackoverflow.com/a/9414495</remarks>
        private static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = Path.GetFileName(shortcutFilename);

            Shell shell = new();
            Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
                return link.Path;
            }

            return string.Empty;
        }
    }
}
