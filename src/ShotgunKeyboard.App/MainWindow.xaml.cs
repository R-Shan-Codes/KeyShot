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

        SoundBankText.Text = $"{Math.Max(1, _audioEngine.NormalSoundVariationCount)} variations";
        UpdateEnabledState();
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

    private void EnableButton_OnClick(object sender, RoutedEventArgs e)
    {
        _audioEngine.Enabled = !_audioEngine.Enabled;
        UpdateEnabledState();
    }

    private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_audioEngine is null)
        {
            return;
        }

        _audioEngine.MasterVolume = (float)(e.NewValue / 100.0);
        VolumeText.Text = $"{Math.Round(e.NewValue):0}%";
    }

    private void UpdateEnabledState()
    {
        if (_audioEngine.Enabled)
        {
            StatusText.Text = "● ACTIVE";
            StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            EnableButton.Content = "Disable KeyShot";
        }
        else
        {
            StatusText.Text = "● OFF";
            StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            EnableButton.Content = "Enable KeyShot";
        }
    }

    private void DisposeResources()
    {
        _keyboardHook.Dispose();
        _audioEngine.Dispose();
    }
}
