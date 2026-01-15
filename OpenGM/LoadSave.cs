using OpenGM.IO;
using OpenGM.Loading;
using System.Reflection;

namespace OpenGM;

public static class LoadSave
{
    // Handles all the file-system sandboxing stuff that GM does
    // C++ runner used as reference, replaced with easier C# function calls where possible

    // TODO: is this linux/mac compatible?

    public static bool SaveFileExists(string _pszFileName)
    {
        var _name = "";
        GetSaveFileName(ref _name, 2048, _pszFileName);
        return FileExists(_name);
    }

    public static bool BundleFileExist(string _pszFileName)
    {
        var _name = "";
        GetBundleFileName(ref _name, 2048, _pszFileName);
        return FileExists(_name);
    }

    public static bool FileExists(string _pFilename)
    {
        return File.Exists(_pFilename);
    }

    public static int GetBundleFileName(ref string name, int size, string _pszFileName)
    {
        name = "";

        if (string.IsNullOrEmpty(_pszFileName))
        {
            return -1;
        }

        var savedCurrentDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Entry.GamePath;

        var fullPath = Path.GetFullPath(_pszFileName);
        var currentDirectory = Environment.CurrentDirectory;
        var filePrePend = GetFilePrePend();

        Environment.CurrentDirectory = savedCurrentDir;

        if (fullPath.StartsWith(currentDirectory))
        {
            name = "";
            name += filePrePend;
            name += fullPath[currentDirectory.Length..];
        }

        if (fullPath.StartsWith(filePrePend))
        {
            name = "";
            name += filePrePend;
            name += fullPath[filePrePend.Length..];
            return 0;
        }

        DebugLog.LogError($"Not allowing file operation at {fullPath}");
        return 0;
    }

    public static int GetSaveFileName(ref string name, int size, string _pszFileName)
    {
        name = "";

        if (string.IsNullOrEmpty(_pszFileName))
        {
            return -1;
        }

        var savedCurrentDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Entry.GamePath;

        var fullPath = Path.GetFullPath(_pszFileName);
        var currentDirectory = Environment.CurrentDirectory;
        // check whitelisted
        var savePrepend = GetSavePrePend();

        Environment.CurrentDirectory = savedCurrentDir;

        if (fullPath.StartsWith(currentDirectory))
        {
            name = "";
            name += savePrepend;
            name += fullPath[currentDirectory.Length..];
            return 0;
        }

        var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        if (fullPath.StartsWith(workingDirectory))
        {
            name = "";
            name += savePrepend;
            name += fullPath[workingDirectory.Length..];
            return 0;
        }

        if (fullPath.StartsWith(savePrepend))
        {
            name = "";
            name += savePrepend;
            name += fullPath[savePrepend.Length..];
            return 0;
        }

        /*if (fullPath.StartsWith(g_pPrevSaveDirectory))
        {
            name = "";
            name += g_pPrevSaveDirectory;
            name += fullPath[g_pPrevSaveDirectory.Length..];
            return 0;
        }*/

        DebugLog.LogError($"Not allowing file operation at {fullPath}");
        return 0;
    }

    public static string g_pSavePrePend = "";
    public static string g_pFilePrePend = "";

    public static string GetSavePrePend()
    {
        if (!string.IsNullOrEmpty(g_pSavePrePend)) // or empty g_pGameProjectName
        {
            return g_pSavePrePend;
        }

        // TODO: check flags in header to use appdata instead of local

        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        g_pSavePrePend = Path.Combine(folder, GameLoader.GeneralInfo.Name) + Path.DirectorySeparatorChar;
        return g_pSavePrePend;
    }

    public static string GetFilePrePend()
    {
        if (!string.IsNullOrEmpty(g_pFilePrePend))
        {
            return g_pFilePrePend;
        }

        g_pFilePrePend = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return g_pFilePrePend;
    }

    public static bool RemoveSaveFile(string _pszFileName)
    {
        var _name = "";
        GetSaveFileName(ref _name, 2048, _pszFileName);

        try
        {
            File.Delete(_name);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
