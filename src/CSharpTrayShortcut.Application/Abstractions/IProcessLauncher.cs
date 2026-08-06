using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Application.Abstractions;

/// <summary>Lance un élément par le shell de Windows (SPEC-LAUNCH-001).</summary>
/// <remarks>
/// Port séparé de <see cref="IShortcutSource"/> parce que lire un dossier et démarrer un
/// processus n'ont ni les mêmes droits, ni les mêmes modes d'échec, ni le même intérêt en
/// test : ici, ce qu'on vérifie est <i>qu'une cible et une seule a été demandée</i>, ce qu'un
/// double enregistre trivialement.
/// </remarks>
public interface IProcessLauncher
{
    /// <summary>
    /// Demande au shell d'ouvrir la cible.
    /// </summary>
    /// <param name="target">Cible à lancer, argument compris.</param>
    /// <returns>
    /// Vrai si le lancement a été accepté par le système ; faux si le shell l'a refusé
    /// (association de fichier absente, exécutable refusé par une stratégie…).
    /// </returns>
    /// <remarks>
    /// Un booléen plutôt qu'une exception : un clic sur un élément qui ne s'ouvre pas est un
    /// incident d'usage, et il ne doit pas remonter jusqu'au gestionnaire d'exceptions non
    /// gérées qui, lui, termine l'application (SPEC-LAUNCH-002).
    /// </remarks>
    bool Launch(LaunchTarget target);
}
