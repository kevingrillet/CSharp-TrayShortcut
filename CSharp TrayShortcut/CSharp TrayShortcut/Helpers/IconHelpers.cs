using System;
using System.Drawing;
using System.IO;

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
            try
            {
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
            catch (Exception)
            {
                // Corrupt icon, path too long, COM/shell failure... no icon is fine.
                return null;
            }
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
            try
            {
                if (File.Exists(path))
                {
                    return new Icon(path);
                }
                else if (File.Exists(Path.Combine("Ressources", path)))
                {
                    return new Icon(Path.Combine("Ressources", path));
                }
            }
            catch (Exception)
            {
                // Invalid / corrupt .ico file: fall back to no icon.
            }
            return null;
        }

        /// <summary>
        /// Get file path from shortcut
        /// </summary>
        /// <param name="shortcutFilename">Shortcut file</param>
        /// <returns>File path of shortcut</returns>
        private static string GetShortcutTargetFile(string shortcutFilename)
        {
            return ShellLinkHelper.ResolveTarget(shortcutFilename);
        }
    }
}
