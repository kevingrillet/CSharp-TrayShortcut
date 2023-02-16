<div align="center">
   <a href="https://github.com/kyechan99/capsule-render">
      <img align="center" src="https://capsule-render.vercel.app/api?type=waving&color=gradient&height=250&section=header&text=CSharp&fontAlign=30&fontAlignY=30&fontSize=80&desc=TrayShortcut&descAlign=60&descAlignY=55&descSize=70" />
   </a>
   <br>
   <a href="https://github.com/dependabot">
      <img align="center" alt="Dependabot" src="https://img.shields.io/badge/dependabot-025E8C?style=for-the-badge&logo=dependabot&logoColor=white" />
   </a>
   <a href="https://www.microsoft.com/fr-fr/windows/">
      <img align="center" alt="Windows" src="https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" />
   </a>
   <a href="https://visualstudio.microsoft.com/fr/">
      <img align="center" alt="Visual Studio" src="https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white" />
   </a>
   <a href="https://learn.microsoft.com/fr-fr/dotnet/">
      <img align="center" alt=".NET" src="https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white" />
   </a>
   <br />
   <a href="https://discord.gg/scdUu3SUQm">
      <img align="center" alt="Discord" src="https://img.shields.io/discord/914218630214983730?label=Discord&logo=Discord" />
   </a>
   <hr>
</div>

# C# TrayShortcut

Petite expérimentation avec les TrayIcon pour remplacer les barres d'outils.

## Configuration

Modifier dans `Configuration\config.json`:

- `Path` du dossier désiré.
- `PathFolderIcon` sera l'icone utilisée pour les dossiers (nécessite un `.ico`)
- `PathTrayIcon` sera l'icone utilisée pour l'application (nécessite un `.ico`)
- `CustomShortcuts` ajoutera des éléments qui ne sont pas dans le dossier dans une catégorie séparée:
  - `Argument` argument à ajouter au lien à exécuter
  - `Image` icone qui sera à côté (nécessite un `.ico`), si `null` sera remplacé par l'icone du `Path`
  - `Path` lien à exécuter
  - `Text` nom affiché

Exemple:

```json
{
    "CustomShortcuts": [
        {
            "Argument": null,
            "Image": null,
            "Path": "C:\\Program Files (x86)\\Notepad++\\notepad++.exe",
            "Text": "Notepad++"
        }
    ],
    "Path": "D:\\Users\\kevin\\Toolbar",
    "PathFolderIcon": "folder_w10.ico",
    "PathTrayIcon": "icon.ico"
}
```

## Licence

```text
/*
 * ----------------------------------------------------------------------------
 * "THE BEER-WARE LICENSE" (Revision 42):
 * kevingrillet wrote this file. As long as you retain this notice you can do
 * whatever you want with this stuff. If we meet some day, and you think this
 * stuff is worth it, you can buy me a beer in return.
 * ----------------------------------------------------------------------------
 */
```

<div align="center">
   <a href="https://github.com/kyechan99/capsule-render">
      <img align="center" src="https://capsule-render.vercel.app/api?section=footer&type=waving&color=gradient&height=100" />
   </a>
</div>
