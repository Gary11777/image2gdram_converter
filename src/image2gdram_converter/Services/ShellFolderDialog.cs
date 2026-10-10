using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace image2gdram_converter.Services;

/// <summary>
/// Выбор каталога через <c>IFileOpenDialog</c>. Подпись поля и кнопка задаются вызывающим:
/// имя в поле ввода не подменяется, иначе оболочка примет его за имя папки.
/// </summary>
internal static class ShellFolderDialog
{
    private const uint FosNoChangeDir = 0x00000008;
    private const uint FosPickFolders = 0x00000020;
    private const uint FosForceFileSystem = 0x00000040;
    private const uint FosPathMustExist = 0x00000800;
    private const uint SigdnFileSysPath = 0x80058000;
    private const int HresultCancelled = unchecked((int)0x800704C7);

    public static string? Pick(string? folder, string title, string fileNameLabel, string okButton)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(fileNameLabel);
        ArgumentException.ThrowIfNullOrEmpty(okButton);

        var dialog = (IFileOpenDialog)new FileOpenDialog();
        try
        {
            dialog.GetOptions(out uint options);
            dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosPathMustExist | FosNoChangeDir);
            dialog.SetTitle(title);
            dialog.SetOkButtonLabel(okButton);
            dialog.SetFileNameLabel(fileNameLabel);
            if (TryCreateItem(folder) is IShellItem start)
            {
                try
                {
                    dialog.SetFolder(start);
                }
                finally
                {
                    Marshal.ReleaseComObject(start);
                }
            }

            int hr = dialog.Show(Owner());
            if (hr == HresultCancelled)
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(hr);
            dialog.GetResult(out IShellItem result);
            try
            {
                result.GetDisplayName(SigdnFileSysPath, out string path);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(result);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    private static nint Owner()
    {
        Window? window = Application.Current?.MainWindow;
        return window is null ? 0 : new WindowInteropHelper(window).Handle;
    }

    private static IShellItem? TryCreateItem(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return null;
        }

        Guid itemId = typeof(IShellItem).GUID;
        int hr = SHCreateItemFromParsingName(folder, 0, in itemId, out IShellItem? item);
        if (hr < 0)
        {
            if (item is not null)
            {
                Marshal.ReleaseComObject(item);
            }

            return null;
        }

        return item;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHCreateItemFromParsingName(
        string path,
        nint bindContext,
        in Guid itemId,
        out IShellItem? item);

    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialog
    {
    }

    /// <summary>Порядок методов — <c>IModalWindow</c>, затем <c>IFileDialog</c> до <c>GetResult</c>.</summary>
    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig]
        int Show(nint owner);

        void SetFileTypes(uint count, nint filters);

        void SetFileTypeIndex(uint index);

        void GetFileTypeIndex(out uint index);

        void Advise(nint events, out uint cookie);

        void Unadvise(uint cookie);

        void SetOptions(uint options);

        void GetOptions(out uint options);

        void SetDefaultFolder(IShellItem item);

        void SetFolder(IShellItem item);

        void GetFolder(out IShellItem item);

        void GetCurrentSelection(out IShellItem item);

        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

        void GetResult(out IShellItem item);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(nint bindContext, in Guid handlerId, in Guid interfaceId, out nint result);

        void GetParent(out IShellItem parent);

        void GetDisplayName(uint nameType, [MarshalAs(UnmanagedType.LPWStr)] out string name);

        void GetAttributes(uint mask, out uint attributes);

        void Compare(IShellItem other, uint hint, out int order);
    }
}
