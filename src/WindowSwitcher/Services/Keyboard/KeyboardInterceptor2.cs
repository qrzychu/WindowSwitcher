using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace WindowSwitcher.Services.Keyboard;

public class KeyboardInterceptor2 : IKeyboardInterceptor
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeyup = 0x0105;

    private readonly Subject<Unit> _signalSubject = new();
    private readonly IntPtr _hookId;
    private bool _consumeNextWinKeyUp;

    public IObservable<Unit> Signal => _signalSubject.AsObservable();

    public KeyboardInterceptor2()
    {
        _hookId = SetHook(HookCallback);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (wParam == WmKeydown || wParam == WmSyskeydown)
            {
                if (vkCode == (int)VirtualKeyStates.VkS)
                {
                    if ((GetAsyncKeyState(VirtualKeyStates.VkLwin) & 0x8000) != 0 ||
                        (GetAsyncKeyState(VirtualKeyStates.VkRwin) & 0x8000) != 0)
                    {
                        _signalSubject.OnNext(Unit.Default);
                        _consumeNextWinKeyUp = true;
                        return 1; // Consume the S key press when Windows key is pressed
                    }
                }
            }
            else if (wParam == WmKeyup || wParam == WmSyskeyup)
            {
                if ((vkCode == (int)VirtualKeyStates.VkLwin || vkCode == (int)VirtualKeyStates.VkRwin) && _consumeNextWinKeyUp)
                {
                    _consumeNextWinKeyUp = false;
                    return 1; // Consume the Windows key up event
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookId);
        _signalSubject.Dispose();
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(VirtualKeyStates nVirtKey);

    private enum VirtualKeyStates
    {
        VkLwin = 0x5B,
        VkRwin = 0x5C,
        VkS = 0x53
    }
}