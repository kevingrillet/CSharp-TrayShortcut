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
    }
}
