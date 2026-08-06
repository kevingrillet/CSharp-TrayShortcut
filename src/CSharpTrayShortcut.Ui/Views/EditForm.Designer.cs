using System.Windows.Forms;

namespace CSharpTrayShortcut.Ui.Views;

/// <summary>Partie générée par le concepteur WinForms.</summary>
/// <remarks>
/// Aucun libellé visible n'est fixé ici : ils sont posés par le constructeur de
/// <see cref="EditForm"/> depuis le catalogue de textes, sinon la fenêtre resterait
/// monolingue (SPEC-UI-LANG-002).
/// </remarks>
public partial class EditForm
{
    private System.ComponentModel.IContainer components = null!;

    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem saveToolStripMenuItem = null!;
    private ToolStripMenuItem deleteRowToolStripMenuItem = null!;
    private ToolStripMenuItem showFileToolStripMenuItem = null!;
    private DataGridView dataGridView = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;

    /// <summary>Libère les ressources du concepteur.</summary>
    /// <param name="disposing">Vrai pour libérer aussi les ressources managées.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Méthode requise par le concepteur — ne pas modifier à la main.</summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip = new MenuStrip();
        saveToolStripMenuItem = new ToolStripMenuItem();
        deleteRowToolStripMenuItem = new ToolStripMenuItem();
        showFileToolStripMenuItem = new ToolStripMenuItem();
        dataGridView = new DataGridView();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // menuStrip
        menuStrip.Items.AddRange([saveToolStripMenuItem, deleteRowToolStripMenuItem, showFileToolStripMenuItem]);
        menuStrip.Location = new System.Drawing.Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new System.Drawing.Size(784, 24);
        menuStrip.TabIndex = 0;

        // saveToolStripMenuItem
        saveToolStripMenuItem.Name = "saveToolStripMenuItem";
        saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        saveToolStripMenuItem.Click += SaveToolStripMenuItem_Click;

        // deleteRowToolStripMenuItem
        deleteRowToolStripMenuItem.Name = "deleteRowToolStripMenuItem";
        deleteRowToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Delete;
        deleteRowToolStripMenuItem.Click += DeleteRowToolStripMenuItem_Click;

        // showFileToolStripMenuItem
        showFileToolStripMenuItem.Name = "showFileToolStripMenuItem";
        showFileToolStripMenuItem.Click += ShowFileToolStripMenuItem_Click;

        // dataGridView
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.EditMode = DataGridViewEditMode.EditOnEnter;
        dataGridView.Location = new System.Drawing.Point(0, 24);
        dataGridView.Name = "dataGridView";
        dataGridView.RowTemplate.Height = 25;
        dataGridView.Size = new System.Drawing.Size(784, 515);
        dataGridView.TabIndex = 1;

        // statusStrip
        statusStrip.Items.AddRange([statusLabel]);
        statusStrip.Location = new System.Drawing.Point(0, 539);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new System.Drawing.Size(784, 22);
        statusStrip.TabIndex = 2;

        // statusLabel
        statusLabel.Name = "statusLabel";

        // EditForm
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(784, 561);
        Controls.Add(dataGridView);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "EditForm";
        StartPosition = FormStartPosition.CenterScreen;

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
