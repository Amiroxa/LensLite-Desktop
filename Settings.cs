using System.Text.Json;
using Microsoft.Win32;

namespace LensLite;

sealed class Settings
{
    public string Hotkey { get; set; } = "Ctrl+Alt+O";
    /// <summary>"fas+eng", "fas" or "eng".</summary>
    public string Languages { get; set; } = "fas+eng";
    public bool AutoCopy { get; set; } = true;

    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LensLite");
    static readonly string FilePath = Path.Combine(Dir, "settings.json");
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new(); }
        catch { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static bool StartWithWindows
    {
        get { using var k = Registry.CurrentUser.OpenSubKey(RunKey); return k?.GetValue("LensLite") != null; }
        set
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey, true)!;
            if (value) k.SetValue("LensLite", $"\"{Environment.ProcessPath}\"");
            else k.DeleteValue("LensLite", false);
        }
    }
}
