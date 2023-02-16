using Newtonsoft.Json;
using System.Diagnostics;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }

        public class MyCustomApplicationContext : ApplicationContext
        {
            private const string _pathConfig = @"Configuration\config.json";
            private readonly NotifyIcon _notificationIcon;
            private Icon _folderIcon;
            private Settings _settings;

            public MyCustomApplicationContext()
            {
                _notificationIcon = new NotifyIcon()
                {
                    ContextMenuStrip = new ContextMenuStrip(),
                    Visible = true
                };
                this.Refresh(null, null);
            }

            private static Icon SetIcon(string path)
            {
                if (!string.IsNullOrEmpty(path))
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
                return null;
            }

            private void AfterGenerateMenu(ContextMenuStrip contextMenuStrip)
            {
                contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem(nameof(Refresh), null, new EventHandler(Refresh)),
                    new ToolStripMenuItem(nameof(Edit), null, new EventHandler(Edit)),
                    new ToolStripMenuItem(nameof(Exit), null, new EventHandler(Exit)),
                });
            }

            private void Edit(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo(@"notepad") { Arguments = _pathConfig, UseShellExecute = true });
                //File.WriteAllText(_pathConfig, JsonConvert.SerializeObject(_settings));
            }

            private void Exit(object sender, EventArgs e)
            {
                _notificationIcon.Visible = false;
                Application.Exit();
            }

            private void GenerateCustomsMenu(ContextMenuStrip contextMenuStrip)
            {
                if (!_settings.Customs.Any())
                    return;

                var menuItem = new ToolStripMenuItem
                {
                    Name = "Customs",
                    Text = "Customs",
                    Image = SystemIcons.Application.ToBitmap()
                };

                foreach (var c in _settings.Customs.OrderBy(c => c.Text))
                {
                    var subMenuItem = new ToolStripMenuItem
                    {
                        Name = c.Name,
                        Text = Path.GetFileNameWithoutExtension(c.Text),
                        Image = !string.IsNullOrEmpty(c.Image) ? new Icon(c.Image).ToBitmap() : Icon.ExtractAssociatedIcon(c.Name).ToBitmap()
                    };
                    subMenuItem.Click += new EventHandler(ItemClick);
                    menuItem.DropDownItems.Add(subMenuItem);
                }

                contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    menuItem,
                });
            }

            private void GenerateMenu(ContextMenuStrip contextMenuStrip, string path = null, ToolStripMenuItem parent = null)
            {
                path ??= _settings.Path;

                var directories = Directory.GetDirectories(path);
                foreach (var d in directories)
                {
                    var menuItem = new ToolStripMenuItem
                    {
                        Name = d,
                        Text = Path.GetFileName(d),
                        Image = _folderIcon.ToBitmap()
                    };

                    this.GenerateMenu(contextMenuStrip, d, menuItem);

                    if (parent == null)
                    {
                        contextMenuStrip.Items.Add(menuItem);
                    }
                    else
                    {
                        parent.DropDownItems.Add(menuItem);
                    }
                }

                if (parent != null)
                {
                    var files = Directory.GetFiles(path);
                    foreach (var f in files)
                    {
                        var subMenuItem = new ToolStripMenuItem
                        {
                            Name = f,
                            Text = Path.GetFileNameWithoutExtension(f),
                            Image = Icon.ExtractAssociatedIcon(f).ToBitmap()
                        };
                        subMenuItem.Click += new EventHandler(ItemClick);
                        parent.DropDownItems.Add(subMenuItem);
                    }
                }
            }

            private void Init()
            {
                if (!Path.Exists(_settings.Path))
                {
                    throw new ApplicationException($"Path does not exist: {_settings.Path}");
                }

                _folderIcon = SetIcon(_settings.PathFolderIcon) ?? new Icon(Path.Combine("Ressources", "folder_w10.ico"));
            }

            private void ItemClick(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo((sender as ToolStripMenuItem).Name) { UseShellExecute = true });
            }

            private void Refresh(object sender, EventArgs e)
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_pathConfig));
                this.Init();
                _notificationIcon.Icon = SetIcon(_settings.PathTrayIcon) ?? new Icon(Path.Combine("Ressources", "icon.ico"));
                var contextMenuStrip = _notificationIcon.ContextMenuStrip;
                contextMenuStrip.Items.Clear();
                this.GenerateMenu(contextMenuStrip);
                this.GenerateCustomsMenu(contextMenuStrip);
                this.AfterGenerateMenu(contextMenuStrip);
            }
        }
    }

    internal class CustomMenu
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
    }

    internal class Settings
    {
        public Settings()
        {
            Customs = new();
        }

        public List<CustomMenu> Customs { get; set; }
        public string Path { get; set; }
        public string PathFolderIcon { get; set; }
        public string PathTrayIcon { get; set; }
    }
}