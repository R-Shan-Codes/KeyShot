namespace ShotgunKeyboard.Core.Input;

internal sealed class KeyPressTracker
{
    private readonly HashSet<uint> _pressedKeys = [];
    private readonly object _sync = new();

    public bool TryPress(uint virtualKeyCode)
    {
        lock (_sync)
        {
            return _pressedKeys.Add(virtualKeyCode);
        }
    }

    public void Release(uint virtualKeyCode)
    {
        lock (_sync)
        {
            _pressedKeys.Remove(virtualKeyCode);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _pressedKeys.Clear();
        }
    }
}
