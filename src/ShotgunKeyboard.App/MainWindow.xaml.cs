using System;
using System.Windows;
using System.Windows.Threading;
using ShotgunKeyboard.Core.Audio;
using ShotgunKeyboard.Core.Input;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly AudioEngine _audioEngine;
    private readonly HashSet<uint> _pressedKeys = [];

    public MainWindow()
    {
        InitializeComponent();

        _keyboardHook = new GlobalKeyboardHook();
        _audioEngine = new AudioEngine();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.KeyUp += OnGlobalKeyUp;
        _keyboardHook.Start();

        Closed += (_, _) =>
        {
            _keyboardHook.Dispose();
            _audioEngine.Dispose();
        };
    }

    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        lock (_pressedKeys)
        {
            if (!_pressedKeys.Add(e.VirtualKeyCode))
            {
                return;
            }
        }

        _audioEngine.PlayShot();

        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            LastKeyText.Text = $"VK {e.VirtualKeyCode}";
        });
    }

    private void OnGlobalKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        lock (_pressedKeys)
        {
            _pressedKeys.Remove(e.VirtualKeyCode);
        }
    }
}
