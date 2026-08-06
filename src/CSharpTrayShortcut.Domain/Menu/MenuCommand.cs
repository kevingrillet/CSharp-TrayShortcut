namespace CSharpTrayShortcut.Domain.Menu;

/// <summary>
/// Actions propres à l'application, présentes en bas du menu (SPEC-MENU-001, règle 5).
/// </summary>
/// <remarks>
/// Une énumération plutôt que des délégués dans le modèle : le menu décrit <i>ce qui est
/// proposé</i>, la couche de présentation décide <i>ce que ça fait</i>. C'est ce qui permet de
/// vérifier la composition du menu dans un test sans aucun gestionnaire d'événement.
/// </remarks>
public enum MenuCommand
{
    /// <summary>Relire la configuration et reconstruire le menu (SPEC-CFG-004).</summary>
    Refresh = 0,

    /// <summary>Ouvrir la fenêtre d'édition des raccourcis personnalisés (SPEC-CFG-003).</summary>
    Edit = 1,

    /// <summary>Quitter l'application.</summary>
    Exit = 2,
}
