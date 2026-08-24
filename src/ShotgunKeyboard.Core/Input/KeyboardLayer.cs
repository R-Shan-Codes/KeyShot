namespace ShotgunKeyboard.Core.Input;

public enum KeyboardLayer
{
    Alphanumeric,
    Function,
    Navigation,
    Numpad,
    Modifiers,
    Arrows
}

public static class KeyboardLayerResolver
{
    public static KeyboardLayer Resolve(uint vk)
    {
        return vk switch
        {
            >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A or 0x20 or 0xBA or 0xBB or 0xBC or 0xBD or 0xBE or 0xBF or 0xC0 or 0xDB or 0xDC or 0xDD or 0xDE => KeyboardLayer.Alphanumeric,
            >= 0x70 and <= 0x7B => KeyboardLayer.Function,
            0x21 or 0x22 or 0x23 or 0x24 or 0x2D or 0x2E => KeyboardLayer.Navigation,
            >= 0x60 and <= 0x69 or 0x6A or 0x6B or 0x6D or 0x6E or 0x6F => KeyboardLayer.Numpad,
            0x10 or 0x11 or 0x12 or 0x14 or 0x5B or 0x5C or 0x5D or 0x5F => KeyboardLayer.Modifiers,
            0x25 or 0x26 or 0x27 or 0x28 => KeyboardLayer.Arrows,
            _ => KeyboardLayer.Alphanumeric
        };
    }
}
