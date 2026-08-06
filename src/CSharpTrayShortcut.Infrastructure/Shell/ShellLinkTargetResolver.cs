using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CSharpTrayShortcut.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.Shell;

/// <summary>
/// Lit la cible d'un raccourci Windows par l'interface COM <c>IShellLink</c> (SPEC-ICON-003).
/// </summary>
/// <remarks>
/// <para>
/// Les interfaces sont déclarées ici par P/Invoke plutôt qu'importées d'une référence COM
/// Shell32 : une référence <c>tlbimp</c> ne se génère que sous le MSBuild du .NET Framework,
/// ce qui empêchait le projet de compiler avec le SDK .NET.
/// </para>
/// <para>
/// Toute défaillance est absorbée et rend <see langword="null"/>, conformément au contrat de
/// <see cref="IShortcutTargetResolver"/> : un raccourci abîmé, une cible sur un partage
/// injoignable ou un refus du shell ne doit coûter qu'une icône, pas le menu.
/// </para>
/// </remarks>
public sealed class ShellLinkTargetResolver : IShortcutTargetResolver
{
    private const int MaxPath = 260;
    private const uint StgmRead = 0x00000000;
    private const uint SlgpRawPath = 0x00000004;

    private readonly ILogger<ShellLinkTargetResolver> _logger;

    /// <summary>Construit le résolveur.</summary>
    /// <param name="logger">Journal, pour tracer les raccourcis illisibles.</param>
    public ShellLinkTargetResolver(ILogger<ShellLinkTargetResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string? ResolveTarget(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath))
        {
            return null;
        }

        IShellLinkW? link = null;
        try
        {
            link = (IShellLinkW)new ShellLink();
            ((IPersistFile)link).Load(shortcutPath, StgmRead);

            var builder = new StringBuilder(MaxPath);
            link.GetPath(builder, builder.Capacity, out _, SlgpRawPath);

            var target = builder.ToString();
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch (Exception ex) when (ex is COMException or IOException or UnauthorizedAccessException)
        {
            // Niveau débogage : sur un dossier bien rempli, un raccourci périmé n'est pas un
            // incident, et le signaler plus fort noierait le journal à chaque ouverture de menu.
            _logger.LogDebug(ex, "Raccourci illisible, icône ignorée : {Chemin}", shortcutPath);
            return null;
        }
        finally
        {
            if (link is not null)
            {
                // Libération immédiate plutôt qu'à la prochaine collecte : un dossier bien
                // rempli en instancie un par raccourci, et les objets COM ne créent pas la
                // pression mémoire qui déclencherait le ramasse-miettes.
                Marshal.FinalReleaseComObject(link);
            }
        }
    }

    /// <summary>
    /// Coclasse <c>ShellLink</c> du shell Windows, instanciée par le runtime COM.
    /// </summary>
    /// <remarks>
    /// Volontairement <b>non scellée</b> : sur un type scellé, le compilateur connaît
    /// statiquement la liste des interfaces implémentées et refuse la conversion vers
    /// <see cref="IShellLinkW"/> (CS0030). Laisser la classe ouverte reporte la vérification au
    /// runtime, qui interroge alors l'objet COM — c'est le mécanisme attendu ici.
    /// </remarks>
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out Win32FindDataW pfd, uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        void Resolve(IntPtr hwnd, uint fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    /// <summary>
    /// Structure attendue par <c>IShellLink::GetPath</c>. Aucun champ n'est lu : elle n'est là
    /// que pour respecter la signature native.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Win32FindDataW
    {
        public uint DwFileAttributes;
        public FILETIME FtCreationTime;
        public FILETIME FtLastAccessTime;
        public FILETIME FtLastWriteTime;
        public uint NFileSizeHigh;
        public uint NFileSizeLow;
        public uint DwReserved0;
        public uint DwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        public string CFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string CAlternateFileName;
    }
}
