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

        LayerComboBox.ItemsSource = Enum.GetValues<KeyboardLayer>();
        LayerComboBox.SelectedItem = KeyboardLayer.Alphanumeric;
        RefreshLayerSoundChoices(activePlaylist);

        VolumeSlider.Value = _settings.Volume;
        SoundBankText.Text = $"{Math.Max(1, _audioEngine.NormalSoundVariationCount)} sounds";
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
        var selectedSound = _audioEngine.Mode == PlaybackMode.Selected
            ? GetLayerSound(layer)
            : null;

        var isEnter = layer == KeyboardLayer.Enter;
        _audioEngine.PlayShot(isEnter, selectedSound);

        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            LastKeyText.Text = $"{layer} · VK {e.VirtualKeyCode}";
        });
    }

    private string? GetLayerSound(KeyboardLayer layer)
    {
        var configured = _settings.GetLayerSound(layer);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return LayerSoundComboBox.ItemsSource is IEnumerable<SoundItem> sounds
            ? sounds.FirstOrDefault()?.FileName
            : null;
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
        SoundBankText.Text = $"{Math.Max(1, _audioEngine.NormalSoundVariationCount)} sounds";
        RefreshLayerSoundChoices(playlist);
    }

    private void PlaybackModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || PlaybackModeComboBox.SelectedItem is not PlaybackMode mode) return;
        _audioEngine.Mode = mode;
        _settings.PlaybackMode = mode;
        _settings.Save();
        UpdateLayerControls();
    }

    private void LayerComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLayerUi || LayerComboBox.SelectedItem is not KeyboardLayer layer)
        {
            return;
        }

        SelectConfiguredLayerSound(layer);
    }

    private void LayerSoundComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingLayerUi || LayerComboBox.SelectedItem is not KeyboardLayer layer || LayerSoundComboBox.SelectedItem is not SoundItem sound)
        {
            return;
        }

        _settings.SetLayerSound(layer, sound.FileName);
        _settings.Save();
    }

    private void RefreshLayerSoundChoices(PlaylistDefinition? playlist)
    {
        _updatingLayerUi = true;
        try
        {
            var sounds = playlist?.Sounds ?? [];
            LayerSoundComboBox.ItemsSource = sounds;
            if (LayerComboBox.SelectedItem is KeyboardLayer layer)
            {
                SelectConfiguredLayerSound(layer);
            }
        }
        finally
        {
            _updatingLayerUi = false;
        }
    }

    private void SelectConfiguredLayerSound(KeyboardLayer layer)
    {
        var configured = _settings.GetLayerSound(layer);
        var sounds = LayerSoundComboBox.ItemsSource as IEnumerable<SoundItem>;
        var selected = sounds?.FirstOrDefault(sound =>
            string.Equals(sound.FileName, configured, StringComparison.OrdinalIgnoreCase))
            ?? sounds?.FirstOrDefault();

        LayerSoundComboBox.SelectedItem = selected;
    }

    private void UpdateLayerControls()
    {
        var enabled = _audioEngine.Mode == PlaybackMode.Selected;
        LayerComboBox.IsEnabled = enabled;
        LayerSoundComboBox.IsEnabled = enabled;
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
