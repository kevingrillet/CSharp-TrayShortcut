using CSharpTrayShortcut.Application.Configuration;

namespace CSharpTrayShortcut.Application.Abstractions;

/// <summary>
/// Persistance de la configuration de l'application (SPEC-CFG-001).
/// </summary>
/// <remarks>
/// Motif Repository : le reste du code ne sait pas qu'il s'agit d'un fichier JSON, ni où il
/// se trouve. C'est ce qui a permis de déplacer la configuration du dossier de l'exécutable
/// vers le dossier de données de l'utilisateur (ADR-0002) sans toucher à une seule règle.
/// </remarks>
public interface IConfigurationStore
{
    /// <summary>
    /// Chemin du fichier de configuration, pour l'afficher ou l'ouvrir dans un éditeur.
    /// </summary>
    string ConfigurationFilePath { get; }

    /// <summary>
    /// Relit la configuration. Un fichier absent, vide ou abîmé rend une configuration par
    /// défaut plutôt qu'une exception (SPEC-CFG-001, règle 3).
    /// </summary>
    TrayShortcutConfiguration Load();

    /// <summary>
    /// Écrit la configuration, en créant le dossier de données au besoin.
    /// </summary>
    /// <returns>Vrai si l'écriture a réussi ; faux si le disque l'a refusée.</returns>
    bool Save(TrayShortcutConfiguration configuration);
}
