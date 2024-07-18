using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Security.Principal;

Log("Hi! I will attempt to set so that your javaw.exe will use the GPU!");

if (!OperatingSystem.IsWindows())
{
    Log("This program is only for Windows, sorry!", LogLevel.Error);
    return -1;
}

const string GPURegKey = @"Software\Microsoft\DirectX\UserGpuPreferences";

var javaExecutables = new List<string>();

var winIdentity = new WindowsPrincipal(WindowsIdentity.GetCurrent());

if (!winIdentity.IsInRole(WindowsBuiltInRole.Administrator))
{
    Log("""
Error: You need to run this program as an administrator
Right click me and use "Run as administrator"

(I need it, since I'm gonna rewrite some stuff in your registry)
""", LogLevel.Error);

    Console.ReadLine();

    return -1;
}

Log("Checking for Overwolf/CurseForge registry keys");
var cfRegKey = Registry.CurrentUser.OpenSubKey(@"Software\Overwolf\CurseForge\");
if (cfRegKey != null)
{
    var cfMCRoot = cfRegKey.GetValue("minecraft_root") as string;

    if (!string.IsNullOrWhiteSpace(cfMCRoot))
    {
        Log($"Found it! Modding path is: {cfMCRoot}");
        var cfJavaExecutables = GetJavaExecutablesFromPath(cfMCRoot, "javaw.exe");
        Log($"Found {cfJavaExecutables.Count} executables, adding to list");
        javaExecutables.AddRange(cfJavaExecutables.Select(j => j.FullName));
    }
}
else
{
    Log("Didn't find Overwolf/CurseForge.. anyhow..", LogLevel.Warning);

    Log("Do you want to enter your own path? [Y/N]");
    var acceptedKeys = new[] { ConsoleKey.Y, ConsoleKey.N };
    ConsoleKey key;
    do
    {
        key = Console.ReadKey(true).Key;
        if (!acceptedKeys.Contains(key))
        {
            Log($"That was {key}, not Y or N, try again..", LogLevel.Error);
        }
    } while (!acceptedKeys.Contains(key));

    if (key == ConsoleKey.N)
    {
        Log("Oh.. Ok, exiting! Bye!");
        return 0;
    }

    Log("Enter the path of your modding folder from CurseForge/Overwolf");
    var path = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(path))
    {
        Log("You didn't enter anything.. exiting..", LogLevel.Error);
        return -1;
    }

    var cfJavaExecutables = GetJavaExecutablesFromPath(path, "javaw.exe");
    Log($"Found {cfJavaExecutables.Count} executables, adding to list");
    javaExecutables.AddRange(cfJavaExecutables.Select(j => j.FullName));
}

if (javaExecutables.Count == 0)
{
    Log("""
Well, this was awkward, we haven't found any "important" java executables.
So.. have a nice day!
""", LogLevel.Warning);
}
else
{
    Log($"Do you want to continue before setting things in the registry? Gonna write {javaExecutables.Count} new keys. [Y/N]");
    var acceptedKeys = new[] { ConsoleKey.Y, ConsoleKey.N };
    ConsoleKey key;
    do
    {
        key = Console.ReadKey(true).Key;
        if (!acceptedKeys.Contains(key))
        {
            Log($"That was {key}, not Y or N, try again..", LogLevel.Error);
        }
    } while (!acceptedKeys.Contains(key));

    if (key == ConsoleKey.N)
    {
        Log("Oh.. Ok, exiting! Bye!");
        return 0;
    }

    Log($"Fixing {javaExecutables.Count} executables in the registry");

    var gpuKey = Registry.CurrentUser.OpenSubKey(GPURegKey, true);
    if (gpuKey == null)
    {
        Log("The required registry key was missing, creating new key for you!", LogLevel.Warning);
        gpuKey = Registry.CurrentUser.CreateSubKey(GPURegKey);
    }

    foreach (var java in javaExecutables)
    {
        Log($"Fixing \"{java}\" for you!");
        gpuKey.SetValue(java, "GpuPreference=2;");
    }

    gpuKey.Close();
    gpuKey.Dispose();
    gpuKey = null;

    gpuKey = Registry.CurrentUser.OpenSubKey(GPURegKey)!;
    var keys = gpuKey.GetValueNames();
    var missingKeys = javaExecutables.Where(j => !keys.Contains(j)).ToList();
    if (missingKeys.Count > 0)
    {
        Log($"Failed to write {missingKeys.Count} keys to the registry", LogLevel.Error);
        foreach (var java in missingKeys)
        {
            Log($"Missing key: {java}", LogLevel.Error);
        }

        Log("You might need to do this manually.. sorry!", LogLevel.Error);
        return -1;
    }

    Log("All done! Happy playing!", LogLevel.Success);
}

Console.ReadLine();

return 0;

ConcurrentStack<FileInfo> GetJavaExecutablesFromPath(string path, string searchPattern = "javaw.exe")
{
    string[] IgnoredFolders = [
        "\\$RECYCLE.BIN",
        "\\System Volume Information",
        "\\Recovery",
        "\\xtp",
        "\\Backups"
    ];

    var files = new ConcurrentStack<FileInfo>();

    var rootFolder = new DirectoryInfo(path);
    var rootFiles = rootFolder.EnumerateFiles(searchPattern);

    foreach (var f in rootFiles)
    {
        files.Push(f);
    }

    var directories = Directory.GetDirectories(path).Where(d => !IgnoredFolders.Any(i => d.EndsWith(i))).ToList();
    foreach (var dir in directories)
    {
        try
        {
            var _files = new DirectoryInfo(dir).EnumerateFiles(searchPattern, SearchOption.AllDirectories).Where(d => !d.Attributes.HasFlag(FileAttributes.Directory)).ToArray();
            if (_files.Length > 0)
            {
                files.PushRange(_files);
            }
        }
        catch
        {
        }
    }

    return files;
}

void Log(string message, LogLevel level = LogLevel.Info)
{
    Console.ForegroundColor = level switch
    {
        LogLevel.Info => ConsoleColor.White,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Success => ConsoleColor.Green,
        _ => ConsoleColor.White
    };

    Console.Write($"[{DateTime.Now:HH:mm:ss} / ");
    Console.Write($"{level.ToString().ToUpper()}]");
    Console.ResetColor();
    Console.WriteLine($" {message}");
}

enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}