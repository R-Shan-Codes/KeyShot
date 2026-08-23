namespace ShotgunKeyboard.Core.Input;

public sealed class GlobalKeyEventArgs : EventArgs
{
    public GlobalKeyEventArgs(uint virtualKeyCode)
    {
        VirtualKeyCode = virtualKeyCode;
    }

    public uint VirtualKeyCode { get; }
}
