using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace FableAstra;

public sealed class ModelSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "";
    public string KeyVariable { get; set; } = "";
}

public sealed class Settings
{
    public double Scale { get; set; } = 1;
    public bool Voice { get; set; } = true;
    public bool Motion { get; set; } = true;
    public bool Topmost { get; set; } = true;
    public bool ExpressiveSprites { get; set; } = true;
    public bool Paused { get; set; }
    public int Volume { get; set; } = 35;
    public int LineSeconds { get; set; } = 10;
    public int BreakSeconds { get; set; } = 45;
    public string Artwork { get; set; } = "original";
    public string Topic { get; set; } = "Surprise me";
    public bool LiveAI { get; set; }
    public string FableVoice { get; set; } = "";
    public string AstraVoice { get; set; } = "";
    public ModelSettings Fable { get; set; } = new() { KeyVariable = "FABLE_API_KEY" };
    public ModelSettings Astra { get; set; } = new() { KeyVariable = "ASTRA_API_KEY" };
    public static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FableAstra");
    public static string DataPath => Path.Combine(DataDir, "settings.json");
    public static Settings Load()
    {
        try
        {
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(DataPath)) ?? new();
            s.Scale = double.IsFinite(s.Scale) ? Math.Clamp(s.Scale, .65, 1.4) : 1;
            s.Volume = Math.Clamp(s.Volume, 0, 100);
            s.LineSeconds = Math.Clamp(s.LineSeconds, 6, 30);
            s.BreakSeconds = Math.Clamp(s.BreakSeconds, 10, 300);
            if (s.Artwork != "original" && s.Artwork != "classic" && s.Artwork != "happy" && s.Artwork != "drink" && s.Artwork != "talk") s.Artwork = "original";
            s.Topmost = true;
            s.Fable ??= new(); s.Astra ??= new();
            return s;
        }
        catch { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(DataDir);
        var temp = DataPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, DataPath, true);
    }
    public Settings Clone() => JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(this))!;
}

public static class Startup
{
    const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(RunPath); return key?.GetValue("FableAstra") is string; }
    }
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunPath);
        if (enabled) key.SetValue("FableAstra", "\"" + Environment.ProcessPath + "\" --startup");
        else key.DeleteValue("FableAstra", false);
    }
}
