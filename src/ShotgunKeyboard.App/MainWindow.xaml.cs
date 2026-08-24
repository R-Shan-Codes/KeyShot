using System.IO;
using System.Windows;
using System.Windows.Threading;
using ShotgunKeyboard.App.Settings;
using ShotgunKeyboard.Core.Audio;
using ShotgunKeyboard.Core.Input;
using ShotgunKeyboard.Core.Playlists;
using ShotgunKeyboard.Core.Weapons;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private const uint EnterVirtualKey = 0x0D;

    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly AudioEngine _audioEngine;
    private readonly KeyShotSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settings = KeyShotSettings.Load();

        // Permanent user playlist location:
        // src/ShotgunKeyboard.App/assets/Playlists/
        var playlistsDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "Playlists");
        var playlists = new PlaylistCatalog(playlistsDirectory);

        var activePlaylist = playlists.Find(_settings.Weapon)
            ?? playlists.Find("shotgun")
            ?? playlists.Find("test-sounds")
            ?? playlists.Playlists.FirstOrDefault();

        var soundDirectory = activePlaylist?.Directory
            ?? Path.Combine(AppContext.BaseDirectory, "assets", "sounds", "shotgun");

        var weapon = WeaponCatalog.Get(_settings.Weapon);
        _audioEngine = new AudioEngine(soundDirectory, playlist: activePlaylist, weapon: weapon)
        {
            Enabled = _settings.Enabled,
            MasterVolume = (float)Math.Clamp(_settings.Volume / 100.0, 0, 1)
        };

        WeaponComboBox.ItemsSource = playlists.Playlists;
        WeaponComboBox.SelectedItem = activePlaylist;
        VolumeSlider.Value = _settings.Volume;
        SoundBankText.Text = $"{Math.Max(1, _audioEngine.NormalSoundVariationCount)} sounds";
        UpdateEnabledState();

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

    private void EnableButton_OnClick(object sender, RoutedEventArgs e)
    {
        _audioEngine.Enabled = !_audioEngine.Enabled;
        _settings.Enabled = _audioEngine.Enabled;
        _settings.Save();
        UpdateEnabledState();
    }

    private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_audioEngine is null || _settings is null)
        {
            return;
        }

        _audioEngine.MasterVolume = (float)(e.NewValue / 100.0);
        _settings.Volume = e.NewValue;
        VolumeText.Text = $"{Math.Round(e.NewValue):0}%";

        if (IsInitialized)
        {
            _settings.Save();
        }
    }

    private void WeaponComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized || WeaponComboBox.SelectedItem is not PlaylistDefinition playlist)
        {
            return;
        }

        _settings.Weapon = playlist.Id;
        _settings.Save();
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
        _settings.Enabled = _audioEngine.Enabled;
        _settings.Volume = _audioEngine.MasterVolume * 100;
        _settings.Save();
        _keyboardHook.Dispose();
        _audioEngine.Dispose();
    }
}
