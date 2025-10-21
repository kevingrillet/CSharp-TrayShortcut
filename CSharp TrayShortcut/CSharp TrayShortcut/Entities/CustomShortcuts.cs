namespace CSharp_TrayShortcut.Entities
{
    /// <summary>
    /// Entity used for custom menus
    /// </summary>
    internal class CustomShortcuts
    {
        /// <summary>
        /// Argument added to Run
        /// </summary>
        public string Argument { get; set; } = string.Empty;

        /// <summary>
        /// Images (need to be ico).
        /// If null, will extract image from path
        /// </summary>
        public string Image { get; set; } = string.Empty;

        /// <summary>
        /// Path to run
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Display text
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
