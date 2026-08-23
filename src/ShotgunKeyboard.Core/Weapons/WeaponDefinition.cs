namespace ShotgunKeyboard.Core.Weapons;

public sealed record WeaponDefinition(
    string Id,
    string DisplayName,
    IReadOnlyList<IReadOnlyList<string>> NormalSounds,
    IReadOnlyList<string> HeavySounds,
    float VolumeMultiplier = 1f);
