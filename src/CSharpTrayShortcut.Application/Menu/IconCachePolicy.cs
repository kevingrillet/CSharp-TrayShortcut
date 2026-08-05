using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Application.Menu;

/// <summary>
/// Décide sous quelle clé une image peut être réutilisée (SPEC-ICON-004).
/// </summary>
/// <remarks>
/// <para>
/// Le choix de la clé est une <b>règle</b>, pas un détail de rendu : c'est elle qui détermine
/// combien d'appels au shell une ouverture de menu coûte. Elle vit donc ici, et se vérifie sans
/// écran — le rendu ne fait que consulter un dictionnaire.
/// </para>
/// <para>
/// La distinction entre document et exécutable est expliquée sur
/// <see cref="IconCacheKey"/>. Elle repose sur une liste d'extensions plutôt que sur une
/// interrogation du système : la question « ce fichier porte-t-il sa propre icône ? » n'a pas
/// de réponse bon marché sous Windows, et se tromper par excès de prudence — traiter un
/// document comme un exécutable — ne coûte qu'une extraction de plus.
/// </para>
/// </remarks>
public sealed class IconCachePolicy
{
    /// <summary>
    /// Extensions dont le fichier porte sa propre icône, et non celle de son type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Volontairement courte et fermée. Elle couvre ce qu'on trouve dans un dossier de
    /// raccourcis : des exécutables, des bibliothèques, des icônes.
    /// </para>
    /// <para>
    /// <c>.lnk</c> n'y figure pas, et ce n'est pas un oubli : un raccourci a déjà été résolu
    /// vers sa cible par <see cref="IconSourceResolver"/> avant d'arriver ici (SPEC-ICON-003).
    /// C'est donc l'extension de la <b>cible</b> qui est examinée.
    /// </para>
    /// </remarks>
    private static readonly HashSet<string> SelfIconExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe",
        ".dll",
        ".ico",
        ".cpl",
        ".scr",
        ".msc",
        ".ocx",
    };

    private readonly IShortcutSource _source;

    /// <summary>Construit la règle.</summary>
    /// <param name="source">Accès au système de fichiers, pour lire les empreintes.</param>
    public IconCachePolicy(IShortcutSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>
    /// Vrai si l'icône du fichier dépend du fichier lui-même, faux si elle ne dépend que de son
    /// extension.
    /// </summary>
    public static bool DependsOnFileContent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        var extension = Path.GetExtension(path);

        // Un fichier sans extension n'a pas d'icône de type : Windows lui donne celle du
        // fichier, ou l'icône générique. On le traite comme un exécutable, par prudence.
        return string.IsNullOrEmpty(extension) || SelfIconExtensions.Contains(extension);
    }

    /// <summary>
    /// Clé sous laquelle l'image de cette source peut être réutilisée, ou
    /// <see langword="null"/> si la source ne donne aucune image.
    /// </summary>
    /// <param name="source">
    /// Source <b>élémentaire</b> — un seul maillon de la chaîne de replis, pas la chaîne
    /// entière. C'est ce qui permet à deux chaînes différentes de partager le maillon qu'elles
    /// ont en commun.
    /// </param>
    public IconCacheKey? KeyFor(IconSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Kind == IconSourceKind.None || string.IsNullOrWhiteSpace(source.Path))
        {
            return null;
        }

        // Un fichier .ico désigné explicitement est toujours propre à son chemin : deux icônes
        // différentes ont le même « .ico » comme extension.
        if (source.Kind == IconSourceKind.IconFile || DependsOnFileContent(source.Path))
        {
            return IconCacheKey.ForFile(source.Kind, source.Path, _source.GetFileStamp(source.Path));
        }

        return IconCacheKey.ForExtension(Path.GetExtension(source.Path));
    }
}
