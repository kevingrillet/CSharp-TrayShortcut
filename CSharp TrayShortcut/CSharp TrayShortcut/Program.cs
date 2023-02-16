using System.Configuration;
using System.Diagnostics;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }

        public class MyCustomApplicationContext : ApplicationContext
        {
            private readonly NotifyIcon _notificationIcon;
            private readonly string _path = ConfigurationManager.AppSettings["path"];
            private readonly string _trayIconPath = ConfigurationManager.AppSettings["icon"];
            private Icon _folderIcon = SystemIcons.WinLogo;
            private Icon _trayIcon = SystemIcons.Application;

            public MyCustomApplicationContext()
            {
                this.Init();
                ContextMenuStrip contextMenuStrip = new();

                this.GenerateMenu(contextMenuStrip);

                contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, new EventHandler(Exit))
                });

                _notificationIcon = new NotifyIcon()
                {
                    Icon = _trayIcon,
                    ContextMenuStrip = contextMenuStrip,
                    Visible = true
                };
            }

            private void Exit(object sender, EventArgs e)
            {
                _notificationIcon.Visible = false;
                Application.Exit();
            }

            private void GenerateMenu(ContextMenuStrip contextMenuStrip, string path = null, ToolStripMenuItem parent = null)
            {
                path ??= _path;

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

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S112:General exceptions should never be thrown", Justification = "<En attente>")]
            private void Init()
            {
                if (!Path.Exists(_path))
                {
                    throw new ApplicationException($"Path does not exist: {_path}");
                }

                if (!string.IsNullOrEmpty(_trayIconPath))
                {
                    if (File.Exists(_trayIconPath))
                    {
                        _trayIcon = new Icon(_path);
                    }
                    else if (File.Exists(Path.Combine("Ressources", _trayIconPath)))
                    {
                        _trayIcon = new Icon(Path.Combine("Ressources", _trayIconPath));
                    }
                }

                string folderIconPath = "folder.ico";
                if (File.Exists(folderIconPath))
                {
                    _folderIcon = new Icon(_path);
                }
                else if (File.Exists(Path.Combine("Ressources", folderIconPath)))
                {
                    _folderIcon = new Icon(Path.Combine("Ressources", folderIconPath));
                }
            }

            private void ItemClick(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo((sender as ToolStripMenuItem).Name) { UseShellExecute = true });
            }
        }
    }
}