namespace CSharpTrayShortcut.Application.Abstractions;

/// <summary>
/// Lit la cible d'un raccourci Windows (<c>.lnk</c>) — SPEC-ICON-003.
/// </summary>
/// <remarks>
/// <para>
/// Isolé dans son propre port parce que l'implémentation passe par l'interface COM
/// <c>IShellLink</c> : intestable en unitaire, indisponible hors Windows, et sans rapport avec
/// l'énumération d'un dossier. La règle qui l'utilise — « pour un raccourci, on montre
/// l'icône de ce qu'il pointe » — est en revanche une règle d'application ordinaire, et un
/// double de test suffit à la vérifier.
/// </para>
/// <para>
/// Un raccourci abîmé, une cible sur un partage réseau injoignable ou un fichier verrouillé
/// rendent <see langword="null"/> plutôt qu'une exception : l'appelant retombe alors sur
/// l'icône du <c>.lnk</c> lui-même.
/// </para>
/// </remarks>
public interface IShortcutTargetResolver
{
    /// <summary>
    /// Chemin visé par le raccourci, ou <see langword="null"/> s'il est illisible.
    /// </summary>
    /// <param name="shortcutPath">Chemin du fichier <c>.lnk</c>.</param>
    string? ResolveTarget(string shortcutPath);
}
