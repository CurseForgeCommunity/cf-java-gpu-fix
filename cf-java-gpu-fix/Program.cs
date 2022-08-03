using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Security.Principal;

Console.WriteLine("Hi! I will attempt to set so that your javaw.exe will use the GPU!");

const string GPURegKey = @"Software\Microsoft\DirectX\UserGpuPreferences";

var javaExecutables = new List<string>();

var winIdentity = new WindowsPrincipal(WindowsIdentity.GetCurrent());

if (!winIdentity.IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine();

    Console.WriteLine("Error: You need to run this program as an administrator");
    Console.WriteLine("Right click me and use \"Run as administrator\"");

    Console.WriteLine();

    Console.WriteLine("(I need it, since I'm gonna rewrite some stuff in your registry)");

    Console.ReadLine();

    return -1;
}

Console.WriteLine();

Console.WriteLine("[INFO] Checking for Overwolf/CurseForge registry keys");
var cfRegKey = Registry.CurrentUser.OpenSubKey(@"Software\Overwolf\CurseForge\");
if (cfRegKey != null)
{
    var cfMCRoot = cfRegKey.GetValue("minecraft_root") as string;

    if (!string.IsNullOrWhiteSpace(cfMCRoot))
    {
        Console.WriteLine($"[INFO] Found it! Modding path is: {cfMCRoot}");
        var cfJavaExecutables = GetJavaExecutablesFromPath(cfMCRoot, "javaw.exe");
        Console.WriteLine($"[INFO] Found {cfJavaExecutables.Count} executables, adding to list");
        javaExecutables.AddRange(cfJavaExecutables.Select(j => j.FullName));
    }
}
else
{
    Console.WriteLine("[INFO] Didn't find Overwolf/CurseForge.. anyhow..");
}

Console.WriteLine();

if (javaExecutables.Count == 0)
{
    Console.WriteLine("[INFO] Well, this was awkward, we haven't found any \"important\" java executables.");
    Console.WriteLine("[INFO] So.. have a nice day!");
}
else
{
    Console.WriteLine($"[INFO] Do you want to continue before setting things in the registry? Gonna write {javaExecutables.Count} new keys. [Y/N]");
    var acceptedKeys = new[] { ConsoleKey.Y, ConsoleKey.N };
    ConsoleKey key;
    do
    {
        key = Console.ReadKey(true).Key;
        if (!acceptedKeys.Contains(key))
        {
            Console.WriteLine($"[ERROR] That was {key}, not Y or N, try again..");
        }
    } while (!acceptedKeys.Contains(key));

    if (key == ConsoleKey.N)
    {
        Console.WriteLine("[INFO] Ok, exiting! Bye!");
        return 0;
    }

    Console.WriteLine($"[INFO] Fixing {javaExecutables.Count} executables");

    var gpuKey = Registry.CurrentUser.OpenSubKey(GPURegKey, true);
    if (gpuKey != null)
    {
        foreach (var java in javaExecutables)
        {
            Console.WriteLine($"[INFO] Fixing \"{java}\" for you!");
            gpuKey.SetValue(java, "GpuPreference=2;");
        }

        gpuKey.Close();
    }

    Console.WriteLine("[INFO] All done! Happy playing!");
}

Console.ReadLine();

return 0;

ConcurrentStack<FileInfo> GetJavaExecutablesFromPath(string path, string searchPattern = "javaw.exe")
{
    string[] IgnoredFolders = new string[] {
        "\\$RECYCLE.BIN",
        "\\System Volume Information",
        "\\Recovery",
        "\\xtp",
        "\\Backups"
    };

    var files = new ConcurrentStack<FileInfo>();

    var rootFolderFiles = new DirectoryInfo(path).GetFiles();

    foreach (var f in rootFolderFiles)
    {
        files.Push(f);
    }

    var directories = Directory.GetDirectories(path).Where(d => !IgnoredFolders.Any(i => d.EndsWith(i))).ToList();
    Parallel.ForEach(directories, dir =>
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
    });

    return files;
}