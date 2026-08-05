using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Ui.Localization;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Ui.Views;

/// <summary>
/// Édition des raccourcis personnalisés (SPEC-CFG-003).
/// </summary>
/// <remarks>
/// <para>
/// Une grille liée à une <see cref="BindingList{T}"/> : c'est la façon la plus économique
/// d'obtenir l'ajout, la modification et la suppression de lignes sans écrire de formulaire.
/// </para>
/// <para>
/// La fenêtre ne valide rien et ne filtre rien : le nettoyage des lignes à moitié remplies
/// appartient au dépôt de configuration (<c>JsonConfigurationStore.Save</c>), de sorte que le
/// fichier soit propre quelle que soit la façon dont il a été modifié — y compris à la main.
/// </para>
/// </remarks>
public sealed partial class EditForm : Form
{
    private readonly IConfigurationStore _store;
    private readonly TextService _texts;
    private readonly ILogger<EditForm> _logger;
    private readonly BindingList<CustomShortcut> _shortcuts;
    private readonly TrayShortcutConfiguration _configuration;

    /// <summary>Construit la fenêtre à partir de la configuration enregistrée.</summary>
    public EditForm(IConfigurationStore store, TextService texts, ILogger<EditForm> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _texts = texts;
        _logger = logger;

        InitializeComponent();
        ApplyTexts();

        _configuration = store.Load();

        // Les chaînes nulles ne sont pas modifiables dans une DataGridView : on les remplace par
        // du vide à l'entrée, et le dépôt refait le chemin inverse à l'enregistrement.
        _shortcuts = new BindingList<CustomShortcut>(
            [.. _configuration.CustomShortcuts.Select(Editable)])
        {
            AllowEdit = true,
            AllowNew = true,
            AllowRemove = true,
            RaiseListChangedEvents = true,
        };

        _shortcuts.AddingNew += (_, e) => e.NewObject = new CustomShortcut
        {
            Argument = string.Empty,
            Image = string.Empty,
            Path = string.Empty,
            Text = string.Empty,
        };

        dataGridView.AutoGenerateColumns = true;
        dataGridView.DataSource = _shortcuts;
        dataGridView.DataBindingComplete += (_, _) => ConfigureColumns();
    }

    /// <summary>Pose les libellés depuis le catalogue de textes (SPEC-UI-LANG-002).</summary>
    private void ApplyTexts()
    {
        Text = _texts.Get(TextKeys.Editor.Title);
        saveToolStripMenuItem.Text = _texts.Get(TextKeys.Editor.Save);
        deleteRowToolStripMenuItem.Text = _texts.Get(TextKeys.Editor.DeleteRow);
        showFileToolStripMenuItem.Text = _texts.Get(TextKeys.Editor.ShowFile);
        statusLabel.Text = _store.ConfigurationFilePath;
    }

    /// <summary>
    /// Renomme et réordonne les colonnes générées automatiquement.
    /// </summary>
    /// <remarks>
    /// L'ordre de génération suit la déclaration des propriétés (Argument, Image, Path, Text),
    /// qui n'est pas l'ordre utile à la saisie : on veut d'abord l'intitulé et le chemin.
    /// </remarks>
    private void ConfigureColumns()
    {
        var ordre = new (string Property, string Key, int Index)[]
        {
            (nameof(CustomShortcut.Text), TextKeys.Editor.ColumnText, 0),
            (nameof(CustomShortcut.Path), TextKeys.Editor.ColumnPath, 1),
            (nameof(CustomShortcut.Argument), TextKeys.Editor.ColumnArgument, 2),
            (nameof(CustomShortcut.Image), TextKeys.Editor.ColumnImage, 3),
        };

        foreach (var (property, key, index) in ordre)
        {
            var colonne = dataGridView.Columns[property];
            if (colonne is null)
            {
                continue;
            }

            colonne.HeaderText = _texts.Get(key);
            colonne.DisplayIndex = index;
            colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colonne.FillWeight = 25;
        }
    }

    private void DeleteRowToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        // Les lignes sélectionnées d'abord ; à défaut, celle où se trouve le curseur. Parcours
        // en ordre décroissant : supprimer par index croissant décale les suivants.
        var indices = dataGridView.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .DefaultIfEmpty(dataGridView.CurrentCell?.RowIndex ?? -1)
            .Where(index => index >= 0 && index < _shortcuts.Count)
            .Distinct()
            .OrderDescending()
            .ToList();

        foreach (var index in indices)
        {
            _shortcuts.RemoveAt(index);
        }
    }

    private void SaveToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        // Sans cela, la cellule en cours d'édition n'est pas encore répercutée dans la liste
        // liée, et la dernière saisie serait perdue.
        dataGridView.EndEdit();
        dataGridView.ClearSelection();
        dataGridView.CurrentCell = null;

        _configuration.CustomShortcuts = [.. _shortcuts];

        if (_store.Save(_configuration))
        {
            Close();
            return;
        }

        MessageBox.Show(
            this,
            _texts.Resolve(TextRef.Of(TextKeys.Error.SaveFailed, _store.ConfigurationFilePath)),
            _texts.Get(TextKeys.AppName),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Ouvre le fichier de configuration dans l'éditeur associé aux fichiers <c>.json</c>.
    /// </summary>
    /// <remarks>
    /// <c>UseShellExecute</c> plutôt que « notepad » en dur : l'utilisateur a peut-être un
    /// éditeur qui colore le JSON, et le Bloc-notes n'est pas garanti présent sur une
    /// installation réduite de Windows.
    /// </remarks>
    private void ShowFileToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_store.ConfigurationFilePath)
            {
                UseShellExecute = true,
            })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Ouverture du fichier de configuration impossible.");

            MessageBox.Show(
                this,
                _texts.Resolve(TextRef.Of(TextKeys.Error.LaunchFailed, _store.ConfigurationFilePath)),
                _texts.Get(TextKeys.AppName),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static CustomShortcut Editable(CustomShortcut source) => new()
    {
        Argument = source.Argument ?? string.Empty,
        Image = source.Image ?? string.Empty,
        Path = source.Path ?? string.Empty,
        Text = source.Text ?? string.Empty,
    };
}
