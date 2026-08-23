using System.Windows;
using System.Windows.Threading;
using ShotgunKeyboard.Core.Input;

namespace ShotgunKeyboard.App;

public partial class MainWindow : Window
{
    private readonly GlobalKeyboardHook _keyboardHook;

    public MainWindow()
    {
        InitializeComponent();

        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.Start();

        Closed += (_, _) => _keyboardHook.Dispose();
    }

    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            LastKeyText.Text = $"VK {e.VirtualKeyCode}";
        });
    }
}
