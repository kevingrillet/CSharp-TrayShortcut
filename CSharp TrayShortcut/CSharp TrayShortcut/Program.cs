using System.Diagnostics;

namespace CSharp_TrayShortcut
{
    internal static class Program
    {
        private const string _path = @"";

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
            private readonly NotifyIcon trayIcon;

            public MyCustomApplicationContext()
            {
                ContextMenuStrip contextMenuStrip = new();

                this.GenerateMenu(contextMenuStrip);

                contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                    new ToolStripSeparator(),
                    new ToolStripMenuItem("Exit", null, new EventHandler(Exit))
                });

                trayIcon = new NotifyIcon()
                {
                    Icon = SystemIcons.Application,
                    ContextMenuStrip = contextMenuStrip,
                    Visible = true
                };
            }

            private void GenerateMenu(ContextMenuStrip contextMenuStrip, string path = _path, ToolStripMenuItem parent = null)
            {
                if (parent != null)
                {
                    var files = Directory.GetFiles(path);
                    foreach (var f in files)
                    {
                        var subMenuItem = new ToolStripMenuItem
                        {
                            Name = f,
                            Text = Path.GetFileName(f),
                            Image = Icon.ExtractAssociatedIcon(f).ToBitmap()
                        };
                        subMenuItem.Click += new EventHandler(ItemClick);
                        parent.DropDownItems.Add(subMenuItem);
                    }
                }

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
            }

            private void Exit(object sender, EventArgs e)
            {
                trayIcon.Visible = false;
                Application.Exit();
            }

            private void ItemClick(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo((sender as ToolStripMenuItem).Name) { UseShellExecute = true });
            }
        }
    }
}