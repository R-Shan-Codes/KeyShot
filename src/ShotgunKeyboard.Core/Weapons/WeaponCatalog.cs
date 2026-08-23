namespace ShotgunKeyboard.Core.Weapons;

public static class WeaponCatalog
{
    public static IReadOnlyList<WeaponDefinition> All { get; } =
    [
        new WeaponDefinition(
            "shotgun",
            "Shotgun",
            [
                ["shot_2.wav", "shot_2.mp3"],
                ["shot_3.wav", "shot_3.mp3"],
                ["shot_4.wav", "shot_4.mp3"],
                ["shot_5.wav", "shot_5.mp3"]
            ],
            ["shot_1.mp3", "shot_1.wav"],
            1f)
    ];

    public static WeaponDefinition Get(string? id)
        => All.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
           ?? All[0];
}
