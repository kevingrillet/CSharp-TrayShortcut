using CSharp_TrayShortcut.Entities;
using CSharp_TrayShortcut.Helpers;
using Microsoft.VisualBasic;

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
                    Text = Path.GetFileNameWithoutExtension(c.Text),
                };
                subMenuItem.Click += new EventHandler(ProcessHelpers.Run);
                menuItem.DropDownItems.Add(subMenuItem);
            }

            contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    menuItem,
                });
        }

        /// <summary>
        /// Generate Menus from path, will create all subMenus if folder found
        /// </summary>
        /// <param name="contextMenuStrip">root contextMenu</param>
        /// <param name="path">if null = _settings.Path</param>
        /// <param name="parent">to create subMenus</param>
        private void GenerateMenu(ContextMenuStrip contextMenuStrip, string path = null, ToolStripMenuItem parent = null)
        {
            path ??= _settings.Path;

            var directories = Directory.GetDirectories(path);
            foreach (var d in directories)
            {
                var menuItem = new ToolStripMenuItem
                {
                    Image = _folderIcon,
                    Text = Path.GetFileName(d),
                };

                GenerateMenu(contextMenuStrip, d, menuItem);

                if (parent == null)
                {
                    contextMenuStrip.Items.Add(menuItem);
                }
                else
                {
                    parent.DropDownItems.Add(menuItem);
                }
            }

            if (parent != null || (_settings.ShowRootFiles ?? true))
            {
                var files = Directory.GetFiles(path);
                foreach (var f in files)
                {
                    var subMenuItem = new CustomToolStripMenuItem
                    {
                        Image = IconHelpers.ExtractIconToBitmap(f),
                        Path = f,
                        Text = Path.GetFileNameWithoutExtension(f),
                    };
                    subMenuItem.Click += new EventHandler(ProcessHelpers.Run);

                    if (parent == null)
                    {
                        contextMenuStrip.Items.Add(subMenuItem);
                    }
                    else
                    {
                        parent.DropDownItems.Add(subMenuItem);
                    }
                }
            }
        }

        /// <summary>
        /// Load _settings from Json. Set Icons. Refresh Menus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ApplicationException">If _settings.Path does not exists, BOOM!</exception>
        private void Refresh(object sender, EventArgs e)
        {
            // Load config file
            _settings = JsonHelpers<Settings>.Load(_pathConfig);

            // Check if Path Exists
            while (!Path.Exists(_settings.Path))
            {
                _settings.Path = Interaction.InputBox("Please enter folder path", $"Path does not exist: {_settings.Path}");
                JsonHelpers<Settings>.Save(_pathConfig, _settings);
            }

            // Load icons
            _folderIcon = IconHelpers.IconPathToBitmap(_settings.PathFolderIcon)
                ?? IconHelpers.IconPathToBitmap(_defaultPathFolderIcon);

            // Set Tray icon
            _notificationIcon.Icon = IconHelpers.SetIcon(_settings.PathTrayIcon)
                ?? IconHelpers.SetIcon(_defaultPathTrayIcon);

            // Update menus
            var contextMenuStrip = _notificationIcon.ContextMenuStrip;
            contextMenuStrip.Items.Clear();
            GenerateMenu(contextMenuStrip);
            GenerateCustomsMenu(contextMenuStrip);

            // Static bottom menus
            contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem(nameof(Refresh), null, new EventHandler(Refresh)),
                    new ToolStripMenuItem(nameof(Edit), null, new EventHandler(Edit)),
                    new ToolStripMenuItem(nameof(Exit), null, new EventHandler(Exit)),
                });
        }
    }
}
