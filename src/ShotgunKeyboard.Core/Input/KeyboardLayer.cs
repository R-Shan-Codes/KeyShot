namespace ShotgunKeyboard.Core.Input;

public enum KeyboardLayer
{
    Alphanumeric,
    Function,
    Navigation,
    Arrow,
    Numpad,
    Modifier,
    Space,
    Enter
}

public static class KeyboardLayerResolver
{
    public static KeyboardLayer Resolve(uint virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            0x20 => KeyboardLayer.Space,
            0x0D => KeyboardLayer.Enter,

            // Function / system row.
            0x1B or 0x2C or 0x90 or 0x91 or 0x13 => KeyboardLayer.Function,
            >= 0x70 and <= 0x7B => KeyboardLayer.Function,

            // Navigation cluster.
            0x21 or 0x22 or 0x23 or 0x24 or 0x2D or 0x2E => KeyboardLayer.Navigation,

            // Arrow cluster.
            0x25 or 0x26 or 0x27 or 0x28 => KeyboardLayer.Arrow,

            // Numpad, including numpad operators and numpad Enter.
            >= 0x60 and <= 0x69 or 0x6A or 0x6B or 0x6C or 0x6D or 0x6E or 0x6F => KeyboardLayer.Numpad,

            // Ctrl, Alt, Windows and application/menu keys. Shift stays with
            // the alphanumeric typing layer.
            0x11 or 0x12 or 0x5B or 0x5C or 0x5D or 0xA2 or 0xA3 or 0xA4 or 0xA5 => KeyboardLayer.Modifier,

            // Letters, numbers, punctuation, Tab, Caps Lock, Shift and
            // Backspace are part of the main typing layer.
            _ => KeyboardLayer.Alphanumeric
        };
    }
}
