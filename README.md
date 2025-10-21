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

## 🇫🇷 Description

Petite expérimentation avec les **TrayIcons** pour remplacer les barres d'outils.

## 🇫🇷 Configuration

Modifier dans `Configuration\config.json` :

- `CustomShortcuts` ajoute des éléments personnalisés qui ne sont pas dans le dossier, dans une catégorie séparée :
  - `Argument` : argument à ajouter à la commande exécutée  
  - `Image` : icône affichée à côté (fichier `.ico` requis). Si `null`, l’icône du `Path` sera utilisée  
  - `Path` : lien ou exécutable à lancer  
  - `Text` : nom affiché dans le menu  
- `Path` : chemin du dossier à afficher  
- `PathFolderIcon` : icône utilisée pour les dossiers (`.ico` requis)  
- `PathTrayIcon` : icône utilisée pour l’application (`.ico` requis)  
- `ShowRootFiles` : `true` ou `false` pour indiquer si les fichiers à la racine du dossier doivent être visibles.  

#### 🇫🇷 Exemple

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
    "PathTrayIcon": "icon.ico",
    "ShowRootFiles": true
}
```

## 🇫🇷 Licence

```text
/*
 * ----------------------------------------------------------------------------
 * "LICENCE BEERWARE" (Révision 42):
 * kevingrillet a créé ce fichier. Tant que vous conservez cet avertissement,
 * vous pouvez faire ce que vous voulez de ce truc. Si on se rencontre un jour et
 * que vous pensez que ce truc vaut le coup, vous pouvez me payer une bière en
 * retour. Poul-Henning Kamp
 * ----------------------------------------------------------------------------
 */
```

---

## 🇬🇧 Description

A small experiment using **TrayIcons** as a replacement for traditional toolbars.

## 🇬🇧 Configuration

Edit `Configuration\config.json`:

- `CustomShortcuts` adds custom items not found in the folder, grouped in a separate category:
  - `Argument`: optional argument to append when executing the target  
  - `Image`: icon displayed next to the item (requires a `.ico` file). If `null`, the icon from `Path` will be used  
  - `Path`: path or executable to launch  
  - `Text`: label shown in the menu  
- `Path`: desired folder path  
- `PathFolderIcon`: icon used for folders (requires `.ico`)  
- `PathTrayIcon`: icon used for the application (requires `.ico`)  
- `ShowRootFiles`: `true` or `false` to show files at the root of `Path`.  

#### 🇬🇧 Example

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
    "PathTrayIcon": "icon.ico",
    "ShowRootFiles": true
}
```

### 🇬🇧 License

```text
/*
 * ----------------------------------------------------------------------------
 * "THE BEER-WARE LICENSE" (Revision 42):
 * kevingrillet wrote this file. As long as you retain this notice you
 * can do whatever you want with this stuff. If we meet some day, and you think
 * this stuff is worth it, you can buy me a beer in return Poul-Henning Kamp
 * ----------------------------------------------------------------------------
 */
```

---

<div align="center">
   <a href="https://github.com/kyechan99/capsule-render">
      <img align="center" src="https://capsule-render.vercel.app/api?section=footer&type=waving&color=gradient&height=100" />
   </a>
</div>
