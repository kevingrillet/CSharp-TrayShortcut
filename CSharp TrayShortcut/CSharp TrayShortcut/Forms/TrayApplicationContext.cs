using CSharp_TrayShortcut.Entities;
using CSharp_TrayShortcut.Helpers;
using Microsoft.VisualBasic;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CSharp_TrayShortcut.Forms
{
    public class TrayApplicationContext : ApplicationContext
    {
        private const string _defaultPathFolderIcon = "folder_w10.ico";
        private const string _defaultPathTrayIcon = "icon.ico";
        private const string _pathConfig = @"Configurations\config.json";
        private readonly NotifyIcon _notificationIcon;
        private Image _folderIcon;
        private Settings _settings;

        public TrayApplicationContext()
        {
            _notificationIcon = new NotifyIcon()
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };
            Refresh(null, null);
        }

        /// <summary>
        /// Open Form to edit config file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Edit(object sender, EventArgs e)
        {
            var editForm = new EditForm(_pathConfig, _settings);
            editForm.FormClosed += (sender, e) => { Refresh(null, null); };
            editForm.Show();
        }

        /// <summary>
        /// Exit app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit(object sender, EventArgs e)
        {
            _notificationIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Generate Menus from _settings.Customs. It will create a Customs folder.
        /// </summary>
        /// <param name="contextMenuStrip">root contextMenu</param>
        private void GenerateCustomsMenu(ContextMenuStrip contextMenuStrip)
        {
            if (_settings.CustomShortcuts.Count == 0)
                return;

            var menuItem = new ToolStripMenuItem
            {
                Text = "Customs",
                Image = SystemIcons.Application.ToBitmap()
            };

            foreach (var c in _settings.CustomShortcuts.OrderBy(c => c.Text))
            {
                var subMenuItem = new CustomToolStripMenuItem
                {
                    Argument = c.Argument,
                    Image = !string.IsNullOrWhiteSpace(c.Image)
                        ? IconHelpers.IconPathToBitmap(c.Image)
                        : IconHelpers.ExtractIconToBitmap(c.Path),
                    Path = c.Path,
                    Text = c.Text,
                };
                subMenuItem.Click += new EventHandler(ProcessHelpers.Run);
                menuItem.DropDownItems.Add(subMenuItem);
            }

            contextMenuStrip.Items.AddRange([
                    new ToolStripSeparator(),
                    menuItem,
                ]);
        }

        /// <summary>
        /// Generate the root menu. Sub-folders are populated lazily when opened
        /// (see <see cref="PopulateDirectory"/> / <see cref="LazyPopulate"/>) so that
        /// startup only enumerates the first level instead of the whole tree.
        /// </summary>
        /// <param name="contextMenuStrip">root contextMenu</param>
        private void GenerateMenu(ContextMenuStrip contextMenuStrip)
        {
            PopulateDirectory(contextMenuStrip.Items, _settings.Path, isRoot: true);
        }

        /// <summary>
        /// Populate a menu collection with the directories and files found at
        /// <paramref name="path"/>. Sub-directories get a placeholder + lazy handler so
        /// their own content is only built the first time they are opened.
        /// </summary>
        /// <param name="items">collection to fill</param>
        /// <param name="path">directory to enumerate</param>
        /// <param name="isRoot">at root, files are only shown when ShowRootFiles is set</param>
        private void PopulateDirectory(ToolStripItemCollection items, string path, bool isRoot)
        {
            foreach (var d in SafeEnumerate(() => Directory.GetDirectories(path)))
            {
                var menuItem = new ToolStripMenuItem
                {
                    Image = _folderIcon,
                    Text = Path.GetFileName(d),
                    Tag = d,
                };

                // Placeholder so the expand arrow is shown; replaced on first open.
                menuItem.DropDownItems.Add(new ToolStripMenuItem());
                menuItem.DropDownOpening += LazyPopulate;

                items.Add(menuItem);
            }

            if (!isRoot || (_settings.ShowRootFiles ?? true))
            {
                foreach (var f in SafeEnumerate(() => Directory.GetFiles(path)))
                {
                    var subMenuItem = new CustomToolStripMenuItem
                    {
                        Image = IconHelpers.ExtractIconToBitmap(f),
                        Path = f,
                        Text = Path.GetFileNameWithoutExtension(f),
                    };
                    subMenuItem.Click += new EventHandler(ProcessHelpers.Run);
                    items.Add(subMenuItem);
                }
            }
        }

        /// <summary>
        /// Builds a directory sub-menu on first open, then unsubscribes so it is built once.
        /// </summary>
        private void LazyPopulate(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not string path)
                return;

            menuItem.DropDownOpening -= LazyPopulate;
            menuItem.DropDownItems.Clear(); // remove placeholder
            PopulateDirectory(menuItem.DropDownItems, path, isRoot: false);
        }

        /// <summary>
        /// Run an enumeration that touches the file system, swallowing access/IO errors
        /// (inaccessible folder, disconnected network drive, deleted directory...) so a
        /// single bad folder never takes the whole application down.
        /// </summary>
        private static string[] SafeEnumerate(Func<string[]> enumerate)
        {
            try
            {
                return enumerate();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                return [];
            }
        }

        /// <summary>
        /// Dispose every image held by the menu items (recursively), skipping the shared
        /// folder icon which is disposed separately. Prevents GDI handle leaks on refresh.
        /// </summary>
        private static void DisposeMenuImages(ToolStripItemCollection items, Image skip)
        {
            if (items == null)
                return;

            foreach (ToolStripItem item in items)
            {
                if (item.Image != null && !ReferenceEquals(item.Image, skip))
                {
                    item.Image.Dispose();
                }

                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    DisposeMenuImages(menuItem.DropDownItems, skip);
                }
            }
        }

        /// <summary>
        /// Load _settings from Json. Set Icons. Refresh Menus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refresh(object sender, EventArgs e)
        {
            // Load config file (fall back to defaults if missing or unreadable)
            _settings = JsonHelpers<Settings>.Load(_pathConfig) ?? new Settings();

            // Check if Path Exists
            while (string.IsNullOrWhiteSpace(_settings.Path) || !Path.Exists(_settings.Path))
            {
                _settings.Path = Interaction.InputBox("Please enter folder path", $"Path does not exist: {_settings.Path}");
                JsonHelpers<Settings>.Save(_pathConfig, _settings);
            }

            var contextMenuStrip = _notificationIcon.ContextMenuStrip;

            // Dispose old menu images (skip current folder icon, disposed just below).
            DisposeMenuImages(contextMenuStrip.Items, _folderIcon);
            contextMenuStrip.Items.Clear();

            // Load folder icon (dispose previous one first).
            _folderIcon?.Dispose();
            _folderIcon = IconHelpers.IconPathToBitmap(_settings.PathFolderIcon)
                ?? IconHelpers.IconPathToBitmap(_defaultPathFolderIcon);

            // Set Tray icon (dispose previous one).
            var previousTrayIcon = _notificationIcon.Icon;
            _notificationIcon.Icon = IconHelpers.SetIcon(_settings.PathTrayIcon)
                ?? IconHelpers.SetIcon(_defaultPathTrayIcon);
            previousTrayIcon?.Dispose();

            // Update menus
            GenerateMenu(contextMenuStrip);
            GenerateCustomsMenu(contextMenuStrip);

            // Static bottom menus
            contextMenuStrip.Items.AddRange([
                    new ToolStripSeparator(),
                    new ToolStripMenuItem(nameof(Refresh), null, new EventHandler(Refresh)),
                    new ToolStripMenuItem(nameof(Edit), null, new EventHandler(Edit)),
                    new ToolStripMenuItem(nameof(Exit), null, new EventHandler(Exit)),
                ]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_notificationIcon != null)
                {
                    DisposeMenuImages(_notificationIcon.ContextMenuStrip?.Items, _folderIcon);
                    _notificationIcon.Icon?.Dispose();
                    _notificationIcon.Dispose();
                }
                _folderIcon?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
