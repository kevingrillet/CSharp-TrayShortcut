using System.Text.Json.Serialization;
using CSharpTrayShortcut.Domain.Text;

namespace CSharpTrayShortcut.Application.Configuration;

/// <summary>
/// Tout ce qui se règle dans l'application (SPEC-CFG-001).
/// </summary>
/// <remarks>
/// Motif Options : un seul objet porte la configuration, et un seul point la valide
/// (<see cref="Validate"/>). Les valeurs par défaut sont portées par les propriétés
/// elles-mêmes, ce qui fait qu'un fichier partiel — ou absent — donne une configuration
/// utilisable sans code de complétion ailleurs.
/// </remarks>
public sealed class TrayShortcutConfiguration
{
    /// <summary>Nom du fichier d'icône de dossier livré avec l'application.</summary>
    public const string DefaultFolderIcon = "folder_w10.ico";

    /// <summary>Nom du fichier d'icône de la zone de notification livré avec l'application.</summary>
    public const string DefaultTrayIcon = "tray-shortcut.ico";

    /// <summary>Raccourcis déclarés à la main (SPEC-MENU-005).</summary>
    public List<CustomShortcut> CustomShortcuts { get; set; } = [];

    /// <summary>Dossier dont le contenu devient le menu.</summary>
    public string? Path { get; set; }

    /// <summary>
    /// Icône affichée devant chaque dossier. À défaut, celle livrée avec l'application
    /// (SPEC-ICON-002).
    /// </summary>
    public string? PathFolderIcon { get; set; }

    /// <summary>
    /// Icône de la zone de notification. À défaut, celle livrée avec l'application
    /// (SPEC-ICON-002).
    /// </summary>
    public string? PathTrayIcon { get; set; }

    /// <summary>
    /// Afficher les fichiers situés à la racine du dossier surveillé (SPEC-MENU-001, règle 3).
    /// </summary>
    /// <remarks>
    /// <see cref="bool"/> nullable pour distinguer « absent du fichier » de « explicitement
    /// faux » : l'absence vaut <see langword="true"/>, ce qui préserve le comportement des
    /// configurations écrites avant l'apparition du réglage.
    /// </remarks>
    public bool? ShowRootFiles { get; set; }

    /// <summary>Langue de l'interface. À défaut, celle de Windows (SPEC-UI-LANG-001).</summary>
    public LanguagePreference Language { get; set; } = LanguagePreference.System;

    /// <summary>Valeur effective de <see cref="ShowRootFiles"/>, absence comprise.</summary>
    /// <remarks>
    /// <see cref="JsonIgnoreAttribute"/> est indispensable : sans lui, cette propriété calculée
    /// se retrouve <b>écrite</b> dans le fichier de configuration, à côté du réglage dont elle
    /// dérive. L'utilisateur y lirait alors un nom de réglage qui n'existe pas — et qui,
    /// n'ayant pas d'accesseur d'écriture, serait ignoré s'il tentait de le modifier.
    /// </remarks>
    [JsonIgnore]
    public bool ShowsRootFiles => ShowRootFiles ?? true;

    /// <summary>
    /// Raison pour laquelle la configuration n'est pas exploitable, ou
    /// <see langword="null"/> si elle l'est (SPEC-CFG-002).
    /// </summary>
    /// <param name="folderExists">
    /// Prédicat d'existence du dossier — injecté plutôt qu'appelé directement, pour que la
    /// validation reste vérifiable sans disque.
    /// </param>
    public TextRef? Validate(Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(folderExists);

        if (string.IsNullOrWhiteSpace(Path))
        {
            return TextRef.Of(TextKeys.Config.PathMissing);
        }

        if (!folderExists(Path))
        {
            return TextRef.Of(TextKeys.Config.PathNotFound, Path);
        }

        return null;
    }
}

/// <summary>Langue demandée pour l'interface (SPEC-UI-LANG-001).</summary>
public enum LanguagePreference
{
    /// <summary>Suivre la langue de Windows, avec repli sur le français.</summary>
    System = 0,

    /// <summary>Français.</summary>
    French = 1,

    /// <summary>Anglais.</summary>
    English = 2,
}
