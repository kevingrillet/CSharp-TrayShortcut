using CSharp_TrayShortcut.Entities;
using CSharp_TrayShortcut.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace CSharp_TrayShortcut.Forms
{
    internal partial class EditForm : Form
    {
        public BindingList<CustomShortcuts> bindingList;
        private readonly string _pathConfig;
        private readonly Settings _settings;

        public EditForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ctor initializing bindingList & dataGridView
        /// </summary>
        /// <param name="pathConfig"></param>
        /// <param name="settings">if null, will be loaded form pathConfig</param>
        public EditForm(string pathConfig, Settings settings = null) : this()
        {
            _pathConfig = pathConfig;
            _settings = settings ?? JsonHelpers<Settings>.Load(_pathConfig);

            // Replace null with empty to enable edit.
            var cs = _settings.CustomShortcuts
                .Select(cs => new CustomShortcuts
                {
                    Argument = cs.Argument ?? string.Empty,
                    Image = cs.Image ?? string.Empty,
                    Path = cs.Path ?? string.Empty,
                    Text = cs.Text ?? string.Empty,
                })
                .ToList();

            // Init binding list
            bindingList = new(cs)
            {
                AllowEdit = true,
                AllowNew = true,
                AllowRemove = true,
                RaiseListChangedEvents = true,
            };
            bindingList.AddingNew += (sender, e) =>
            {
                // If property is null, it can't be adited -_-
                e.NewObject = new CustomShortcuts();
            };

            // Complete DataGridView
            dataGridView.DataSource = bindingList;
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = 25;
            }
        }

        /// <summary>
        /// Delete row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow item in dataGridView.SelectedRows)
                {
                    dataGridView.Rows.RemoveAt(item.Index);
                }
            }
            else
            {
                if (dataGridView.CurrentCell != null)
                {
                    dataGridView.Rows.RemoveAt(dataGridView.CurrentCell.RowIndex);
                }
            }
        }

        /// <summary>
        /// Save & close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear selection to update the bindingList
            dataGridView.ClearSelection();
            dataGridView.CurrentCell = null;

            // Remove empty path, replace empty with null
            _settings.CustomShortcuts = bindingList.AsEnumerable()
                .Where(cs => !string.IsNullOrWhiteSpace(cs.Path))
                .Select(cs => new CustomShortcuts
                {
                    Argument = string.IsNullOrWhiteSpace(cs.Argument) ? null : cs.Argument,
                    Image = string.IsNullOrWhiteSpace(cs.Image) ? null : cs.Image,
                    Path = cs.Path,
                    Text = string.IsNullOrWhiteSpace(cs.Text) ? null : cs.Text,
                })
                .ToList();
            JsonHelpers<Settings>.Save(_pathConfig, _settings);
            Close();
        }

        /// <summary>
        /// Open file in notpad
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"notepad")
            {
                Arguments = _pathConfig,
                UseShellExecute = true
            });
        }
    }
}
