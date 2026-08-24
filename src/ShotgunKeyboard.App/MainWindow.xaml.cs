using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ShotgunKeyboard.App.Settings;
using ShotgunKeyboard.Core.Audio;
using ShotgunKeyboard.Core.Input;
using ShotgunKeyboard.Core.Playlists;
using ShotgunKeyboard.Core.Weapons;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly AudioEngine _audioEngine;
    private readonly KeyShotSettings _settings;
    private readonly PlaylistCatalog _playlists;
    private bool _updatingLayerUi;

    private static readonly (KeyboardLayer Layer, string Name)[] LayerMappings =
    [
        (KeyboardLayer.Alphanumeric, nameof(AlphanumericSoundComboBox)),
        (KeyboardLayer.Function, nameof(FunctionSoundComboBox)),
        (KeyboardLayer.Navigation, nameof(NavigationSoundComboBox)),
        (KeyboardLayer.Arrow, nameof(ArrowsSoundComboBox)),
        (KeyboardLayer.Numpad, nameof(NumpadSoundComboBox)),
        (KeyboardLayer.Modifier, nameof(ModifiersSoundComboBox)),
        (KeyboardLayer.Space, nameof(SpaceSoundComboBox)),
        (KeyboardLayer.Enter, nameof(EnterSoundComboBox))
    ];

    public MainWindow()
    {
        InitializeComponent();
        _settings = KeyShotSettings.Load();
        var playlistsDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "Playlists");
        _playlists = new PlaylistCatalog(playlistsDirectory);
        var activePlaylist = _playlists.Find(_settings.Weapon) ?? _playlists.Find("shotgun") ?? _playlists.Find("test-sounds") ?? _playlists.Playlists.FirstOrDefault();
        var soundDirectory = activePlaylist?.Directory ?? Path.Combine(AppContext.BaseDirectory, "assets", "sounds", "shotgun");
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
        RefreshLayerMappings(activePlaylist);
        VolumeSlider.Value = _settings.Volume;
        UpdateLayerControls();
        UpdateEnabledState();

        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.Start();
        Closed += (_, _) => DisposeResources();
    }

    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        var layer = KeyboardLayerResolver.Resolve(e.VirtualKeyCode);
        var selectedSound = _audioEngine.Mode == PlaybackMode.KeyMapped ? _settings.GetLayerSound(layer) : null;
        _audioEngine.PlayShot(layer == KeyboardLayer.Enter, e.VirtualKeyCode);
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            LastKeyText.Text = $"{layer} · VK {e.VirtualKeyCode}";
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

    private void WeaponComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || WeaponComboBox.SelectedItem is not PlaylistDefinition playlist) return;
        _audioEngine.SetPlaylist(playlist);
        _settings.Weapon = playlist.Id;
        _settings.Save();
        RefreshLayerMappings(playlist);
    }

    private void PlaybackModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || PlaybackModeComboBox.SelectedItem is not PlaybackMode mode) return;
        _audioEngine.Mode = mode;
        _settings.PlaybackMode = mode;
        _settings.Save();
        UpdateLayerControls();
    }

    private void LayerMappingSound_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLayerUi || sender is not ComboBox combo || combo.Tag is not string layerName || combo.SelectedItem is not SoundItem sound) return;
        if (Enum.TryParse<KeyboardLayer>(layerName, out var layer))
        {
            _settings.SetLayerSound(layer, sound.FileName);
            _settings.Save();
        }
    }

    private void RefreshLayerMappings(PlaylistDefinition? playlist)
    {
        _updatingLayerUi = true;
        try
        {
            var sounds = playlist?.Sounds ?? [];
            foreach (var (layer, name) in LayerMappings)
            {
                if (FindName(name) is not ComboBox combo) continue;
                combo.ItemsSource = sounds;
                var configured = _settings.GetLayerSound(layer);
                combo.SelectedItem = sounds.FirstOrDefault(s => string.Equals(s.FileName, configured, StringComparison.OrdinalIgnoreCase))
                    ?? sounds.FirstOrDefault();
            }
        }
        finally
        {
            _updatingLayerUi = false;
        }
    }

    private void UpdateLayerControls()
    {
        var enabled = _audioEngine.Mode == PlaybackMode.KeyMapped;
        foreach (var (_, name) in LayerMappings)
        {
            if (FindName(name) is ComboBox combo) combo.IsEnabled = enabled;
        }
    }

    private void UpdateEnabledState()
    {
        StatusText.Text = _audioEngine.Enabled ? "● ACTIVE" : "● OFF";
        StatusText.Foreground = _audioEngine.Enabled ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Gray;
        EnableButton.Content = _audioEngine.Enabled ? "Disable KeyShot" : "Enable KeyShot";
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
