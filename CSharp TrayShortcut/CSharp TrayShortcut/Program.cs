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
            private readonly string _iconPath = ConfigurationManager.AppSettings["icon"];
            private readonly string _path = ConfigurationManager.AppSettings["path"];
            private readonly NotifyIcon trayIcon;
            private Icon _icon = SystemIcons.Application;

            public MyCustomApplicationContext()
            {
                this.Init();
                ContextMenuStrip contextMenuStrip = new();

                this.GenerateMenu(contextMenuStrip);

                contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, new EventHandler(Exit))
                });

                trayIcon = new NotifyIcon()
                {
                    Icon = _icon,
                    ContextMenuStrip = contextMenuStrip,
                    Visible = true
                };
            }

            private void Exit(object sender, EventArgs e)
            {
                trayIcon.Visible = false;
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
                if (!string.IsNullOrEmpty(_iconPath))
                {
                    if (File.Exists(_iconPath))
                    {
                        _icon = new Icon(_path);
                    }
                    else if (File.Exists(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), _iconPath)))
                    {
                        _icon = new Icon(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), _iconPath));
                    }
                    else if (File.Exists(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), @"..\..\..", _iconPath)))
                    {
                        _icon = new Icon(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), @"..\..\..", _iconPath));
                    }
                }
            }

            private void ItemClick(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo((sender as ToolStripMenuItem).Name) { UseShellExecute = true });
            }
        }
    }
}