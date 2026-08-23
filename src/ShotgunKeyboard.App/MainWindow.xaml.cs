using System.Windows;
using System.Windows.Threading;
using ShotgunKeyboard.Core.Audio;
using ShotgunKeyboard.Core.Input;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly AudioEngine _audioEngine;

    public MainWindow()
    {
        InitializeComponent();

        _audioEngine = new AudioEngine();
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.Start();

        Closed += (_, _) => DisposeResources();
    }

    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        _audioEngine.PlayShot();

        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            LastKeyText.Text = $"VK {e.VirtualKeyCode}";
        });
    }

    private void DisposeResources()
    {
        _keyboardHook.Dispose();
        _audioEngine.Dispose();
    }
}
