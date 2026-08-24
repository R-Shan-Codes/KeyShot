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
    private readonly PlaylistCatalog _playlists;

    public MainWindow()
    {
        InitializeComponent();
        _settings = KeyShotSettings.Load();

        var playlistsDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "Playlists");
        _playlists = new PlaylistCatalog(playlistsDirectory);

        var activePlaylist = _playlists.Find(_settings.Weapon)
            ?? _playlists.Find("shotgun")
            ?? _playlists.Find("test-sounds")
            ?? _playlists.Playlists.FirstOrDefault();

        var soundDirectory = activePlaylist?.Directory
            ?? Path.Combine(AppContext.BaseDirectory, "assets", "sounds", "shotgun");
        var weapon = WeaponCatalog.Get(_settings.Weapon);

        _audioEngine = new AudioEngine(soundDirectory, playlist: activePlaylist, weapon: weapon)
        {
            Enabled = _settings.Enabled,
            MasterVolume = (float)Math.Clamp(_settings.Volume / 100.0, 0, 1),
            Mode = _settings.PlaybackMode
        };

        WeaponComboBox.ItemsSource = _playlists.Playlists;
        WeaponComboBox.SelectedItem = activePlaylist;
        PlaybackModeComboBox.ItemsSource = Enum.GetValues<PlaybackMode>();
        PlaybackModeComboBox.SelectedItem = _settings.PlaybackMode;
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
        _audioEngine.PlayShot(e.VirtualKeyCode == EnterVirtualKey, e.VirtualKeyCode);
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
        if (_audioEngine is null || _settings is null) return;
        _audioEngine.MasterVolume = (float)(e.NewValue / 100.0);
        _settings.Volume = e.NewValue;
        VolumeText.Text = $"{Math.Round(e.NewValue):0}%";
        if (IsInitialized) _settings.Save();
    }

    private void WeaponComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized || WeaponComboBox.SelectedItem is not PlaylistDefinition playlist) return;
        _audioEngine.SetPlaylist(playlist);
        _settings.Weapon = playlist.Id;
        _settings.Save();
        SoundBankText.Text = $"{Math.Max(1, _audioEngine.NormalSoundVariationCount)} sounds";
    }

    private void PlaybackModeComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized || PlaybackModeComboBox.SelectedItem is not PlaybackMode mode) return;
        _audioEngine.Mode = mode;
        _settings.PlaybackMode = mode;
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
