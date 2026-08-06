using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Tests.Doubles;

/// <summary>
/// Résolveur de raccourcis Windows en mémoire (SPEC-ICON-003).
/// </summary>
/// <remarks>
/// Par défaut, aucun raccourci n'est résolvable : c'est le cas le plus fréquent en test, et
/// celui qui doit faire retomber la règle sur l'icône du <c>.lnk</c> lui-même.
/// </remarks>
internal sealed class FakeShortcutTargetResolver : IShortcutTargetResolver
{
    private readonly Dictionary<string, string?> _cibles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Déclare la cible d'un raccourci.</summary>
    internal FakeShortcutTargetResolver Cible(string raccourci, string? cible)
    {
        _cibles[raccourci] = cible;
        return this;
    }

    /// <inheritdoc />
    public string? ResolveTarget(string shortcutPath)
        => _cibles.TryGetValue(shortcutPath, out var cible) ? cible : null;
}

/// <summary>
/// Lanceur qui n'exécute rien mais retient ce qu'on lui a demandé (SPEC-LAUNCH-001).
/// </summary>
internal sealed class FakeProcessLauncher : IProcessLauncher
{
    /// <summary>Cibles reçues, dans l'ordre.</summary>
    internal List<LaunchTarget> Demandes { get; } = [];

    /// <summary>Ce que le lanceur répond. Faux simule un refus du shell.</summary>
    internal bool Accepte { get; set; } = true;

    /// <inheritdoc />
    public bool Launch(LaunchTarget target)
    {
        Demandes.Add(target);
        return Accepte;
    }
}
