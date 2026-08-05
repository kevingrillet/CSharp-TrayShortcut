namespace CSharpTrayShortcut.Domain.Shortcuts;

/// <summary>
/// Ce qu'il faut pour lancer quelque chose : un chemin, et éventuellement un argument
/// (SPEC-LAUNCH-001).
/// </summary>
/// <remarks>
/// <para>
/// Objet-valeur, et non deux chaînes promenées côte à côte : le seul moyen d'en obtenir un
/// est <see cref="TryCreate"/>, qui refuse un chemin vide. Un <see cref="LaunchTarget"/> qui
/// existe porte donc toujours un chemin exploitable, et le code de lancement n'a plus à s'en
/// assurer — c'est ce qui remplace les exceptions que l'ancien gestionnaire de clic levait au
/// beau milieu d'un menu.
/// </para>
/// <para>
/// « Exploitable » ne veut pas dire « existant » : le fichier peut avoir disparu depuis la
/// construction du menu. Ce cas relève du lancement (SPEC-LAUNCH-002), pas de la
/// construction.
/// </para>
/// </remarks>
public sealed record LaunchTarget
{
    private LaunchTarget()
    {
    }

    /// <summary>Chemin de l'élément à lancer. Jamais vide.</summary>
    public required string Path { get; init; }

    /// <summary>
    /// Argument passé au lancement, ou <see langword="null"/> s'il n'y en a pas.
    /// </summary>
    /// <remarks>
    /// Une chaîne vide et une absence d'argument sont la même chose : les deux se normalisent
    /// en <see langword="null"/>, ce qui évite d'avoir à traiter les deux cas plus loin.
    /// </remarks>
    public string? Argument { get; init; }

    /// <summary>
    /// Construit une cible, ou renvoie <see langword="null"/> si le chemin est absent.
    /// </summary>
    /// <param name="path">Chemin de l'élément à lancer.</param>
    /// <param name="argument">Argument facultatif.</param>
    /// <remarks>
    /// Le motif « TryCreate qui renvoie null » plutôt qu'une exception : un chemin manquant
    /// est un cas de configuration ordinaire — une ligne laissée à moitié remplie dans la
    /// fenêtre d'édition —, pas une anomalie de programmation. L'appelant filtre
    /// silencieusement (SPEC-MENU-005, règle 1).
    /// </remarks>
    public static LaunchTarget? TryCreate(string? path, string? argument = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return new LaunchTarget
        {
            Path = path,
            Argument = string.IsNullOrWhiteSpace(argument) ? null : argument,
        };
    }
}
