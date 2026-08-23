using System;
using System.Runtime.InteropServices;

namespace ShotgunKeyboard.Core.Input;

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;

    private readonly LowLevelKeyboardProc _callback;
    private nint _hookHandle;

    public event EventHandler<GlobalKeyEventArgs>? KeyDown;

    public GlobalKeyboardHook()
    {
        _callback = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(null), 0);
        if (_hookHandle == 0)
        {
            throw new InvalidOperationException($"Unable to install the global keyboard hook. Win32 error: {Marshal.GetLastWin32Error()}.");
        }
    }

    public void Stop()
    {
        if (_hookHandle == 0)
        {
            return;
        }

        if (!UnhookWindowsHookEx(_hookHandle))
        {
            throw new InvalidOperationException($"Unable to remove the global keyboard hook. Win32 error: {Marshal.GetLastWin32Error()}.");
        }

        _hookHandle = 0;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && (wParam == WmKeyDown || wParam == WmSysKeyDown))
        {
            var hookStruct = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
            KeyDown?.Invoke(this, new GlobalKeyEventArgs(hookStruct.VkCode));
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Kbdllhookstruct
    {
        public readonly uint VkCode;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly nint DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
