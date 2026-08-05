using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Application.Abstractions;

/// <summary>
/// Source des éléments à présenter dans le menu — en pratique un dossier du disque
/// (SPEC-MENU-001).
/// </summary>
/// <remarks>
/// <para>
/// Ce port existe pour une raison précise : rendre la composition du menu vérifiable. Un
/// double de test décrit une arborescence en trois lignes, là où un test sur le vrai système
/// de fichiers demanderait de créer des dossiers temporaires, donc d'être lent, dépendant des
/// droits, et incapable de simuler un lecteur réseau coupé.
/// </para>
/// <para>
/// <b>Contrat de tolérance.</b> Aucune de ces méthodes ne lève d'exception pour un chemin
/// illisible : un dossier inaccessible, un lecteur réseau déconnecté ou un dossier supprimé
/// entre-temps rend une liste vide (SPEC-MENU-004). C'est l'adaptateur qui absorbe l'erreur,
/// et non chaque appelant — un mauvais dossier ne doit jamais faire tomber le menu entier.
/// </para>
/// </remarks>
public interface IShortcutSource
{
    /// <summary>
    /// Chemins complets des sous-dossiers directs de <paramref name="path"/>, ou une liste
    /// vide si le dossier est illisible.
    /// </summary>
    IReadOnlyList<string> GetDirectories(string path);

    /// <summary>
    /// Chemins complets des fichiers directement contenus dans <paramref name="path"/>, ou
    /// une liste vide si le dossier est illisible.
    /// </summary>
    IReadOnlyList<string> GetFiles(string path);

    /// <summary>
    /// Vrai si le dossier existe et est accessible. Faux — jamais d'exception — dans tous les
    /// autres cas (SPEC-CFG-002).
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Vrai si le fichier existe et est accessible. Faux — jamais d'exception — dans tous les
    /// autres cas.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Empreinte du fichier, ou <see langword="null"/> s'il est absent ou illisible
    /// (SPEC-ICON-004).
    /// </summary>
    /// <remarks>
    /// Une lecture de métadonnées, pas de contenu : c'est ce qui permet de savoir qu'une image
    /// mise en cache est encore valable sans payer une nouvelle extraction d'icône. Une
    /// empreinte absente signifie « je ne sais pas » ; l'appelant traite alors l'entrée de
    /// cache comme périmée, ce qui est le choix sûr.
    /// </remarks>
    FileStamp? GetFileStamp(string path);
}
