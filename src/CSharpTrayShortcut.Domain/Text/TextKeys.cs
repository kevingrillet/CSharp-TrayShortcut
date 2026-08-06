using CSharpTrayShortcut.Domain.Menu;

namespace CSharpTrayShortcut.Domain.Text;

/// <summary>Clés du catalogue de textes (SPEC-UI-LANG-002).</summary>
/// <remarks>
/// <para>
/// Des constantes plutôt que des chaînes libres : une clé mal orthographiée devient une
/// erreur de compilation au lieu d'un message manquant découvert par l'utilisateur. Les clés
/// dérivées d'une énumération passent par une méthode, ce qui garantit qu'une valeur ajoutée
/// à l'énumération est traitée partout de la même façon.
/// </para>
/// <para>
/// Ce fichier vit dans le domaine parce que les quatre couches y font référence, et qu'il ne
/// contient que des identifiants — aucune formulation, aucune langue.
/// </para>
/// </remarks>
public static class TextKeys
{
    /// <summary>Fragment vide (partie facultative absente).</summary>
    public const string Empty = "Common.Empty";

    /// <summary>Nom du produit, identique dans toutes les langues.</summary>
    public const string AppName = "Common.AppName";

    /// <summary>Clé de l'intitulé d'une commande du menu.</summary>
    public static string MenuCommandLabel(MenuCommand command) => $"Menu.{command}";

    /// <summary>Intitulés du menu de la zone de notification.</summary>
    public static class Menu
    {
        /// <summary>Section regroupant les raccourcis personnalisés (SPEC-MENU-005).</summary>
        public const string Customs = "Menu.Customs";

        /// <summary>Info-bulle de l'icône de la zone de notification.</summary>
        public const string Tooltip = "Menu.Tooltip";

        /// <summary>Entrée affichée quand le dossier surveillé est vide.</summary>
        public const string Empty = "Menu.Empty";
    }

    /// <summary>Textes de la fenêtre d'édition des raccourcis personnalisés (SPEC-CFG-003).</summary>
    public static class Editor
    {
        /// <summary>Titre de la fenêtre.</summary>
        public const string Title = "Editor.Title";

        /// <summary>Commande d'enregistrement.</summary>
        public const string Save = "Editor.Save";

        /// <summary>Commande de suppression de la ligne courante.</summary>
        public const string DeleteRow = "Editor.DeleteRow";

        /// <summary>Commande d'ouverture du fichier de configuration.</summary>
        public const string ShowFile = "Editor.ShowFile";

        /// <summary>Colonne « intitulé affiché ».</summary>
        public const string ColumnText = "Editor.Column.Text";

        /// <summary>Colonne « chemin à lancer ».</summary>
        public const string ColumnPath = "Editor.Column.Path";

        /// <summary>Colonne « argument ».</summary>
        public const string ColumnArgument = "Editor.Column.Argument";

        /// <summary>Colonne « icône ».</summary>
        public const string ColumnImage = "Editor.Column.Image";
    }

    /// <summary>Messages de configuration et de validation (SPEC-CFG-002).</summary>
    public static class Config
    {
        /// <summary>Invite de saisie du dossier à surveiller.</summary>
        public const string FolderPrompt = "Config.FolderPrompt";

        /// <summary>Titre de l'invite quand le dossier configuré n'existe pas.</summary>
        public const string FolderMissing = "Config.FolderMissing";

        /// <summary>Titre de l'invite quand aucun dossier n'est encore configuré.</summary>
        public const string FolderUnset = "Config.FolderUnset";

        /// <summary>Le dossier surveillé n'est pas renseigné.</summary>
        public const string PathMissing = "Config.PathMissing";

        /// <summary>Le dossier surveillé n'existe pas ou n'est pas accessible.</summary>
        public const string PathNotFound = "Config.PathNotFound";
    }

    /// <summary>Messages d'erreur techniques présentés à l'utilisateur.</summary>
    public static class Error
    {
        /// <summary>Le lancement d'un élément a échoué.</summary>
        public const string LaunchFailed = "Error.LaunchFailed";

        /// <summary>La configuration n'a pas pu être enregistrée.</summary>
        public const string SaveFailed = "Error.SaveFailed";

        /// <summary>Erreur fatale au démarrage (SPEC-APP-002).</summary>
        public const string Fatal = "Error.Fatal";
    }
}
