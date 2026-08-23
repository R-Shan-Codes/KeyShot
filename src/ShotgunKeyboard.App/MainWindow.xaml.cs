using System.IO;
using System.Windows;
using System.Windows.Threading;
using ShotgunKeyboard.Core.Audio;
using ShotgunKeyboard.Core.Input;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private const uint EnterVirtualKey = 0x0D;

    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly AudioEngine _audioEngine;

    public MainWindow()
    {
        InitializeComponent();

        var soundDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "sounds", "shotgun");
        _audioEngine = new AudioEngine(soundDirectory);
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.Start();

        Closed += (_, _) => DisposeResources();
    }

    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        _audioEngine.PlayShot(e.VirtualKeyCode == EnterVirtualKey);

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
