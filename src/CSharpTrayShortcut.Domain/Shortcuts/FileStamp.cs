namespace CSharpTrayShortcut.Domain.Shortcuts;

/// <summary>
/// Empreinte d'un fichier : ce qui change quand son contenu change (SPEC-ICON-004).
/// </summary>
/// <param name="LastWriteUtc">Date de dernière écriture, en temps universel.</param>
/// <param name="Length">Taille en octets.</param>
/// <remarks>
/// <para>
/// Sert à savoir si une image mise en cache est encore valable. La date seule ne suffit pas :
/// certains outils de déploiement préservent l'horodatage d'un fichier remplacé. La taille
/// seule ne suffit pas non plus. Les deux ensemble suffisent largement pour un menu de
/// raccourcis — on ne cherche pas une empreinte cryptographique, qui coûterait une lecture
/// complète du fichier là où l'on veut justement éviter de le lire.
/// </para>
/// <para>
/// <c>readonly record struct</c> : deux empreintes se comparent par valeur, et l'objet ne coûte
/// aucune allocation.
/// </para>
/// </remarks>
public readonly record struct FileStamp(DateTime LastWriteUtc, long Length);
