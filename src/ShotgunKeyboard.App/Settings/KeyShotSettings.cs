using System.IO;
using System.Text.Json;

namespace ShotgunKeyboard.App.Settings;

public sealed class KeyShotSettings
{
    public bool Enabled { get; set; } = true;
    public double Volume { get; set; } = 78;
    public string Weapon { get; set; } = "shotgun";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyShot",
        "settings.json");

    public static KeyShotSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new KeyShotSettings();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<KeyShotSettings>(json, JsonOptions) ?? new KeyShotSettings();
        }
        catch
        {
            return new KeyShotSettings();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
