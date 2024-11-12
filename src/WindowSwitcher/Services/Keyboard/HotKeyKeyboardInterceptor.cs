using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace WindowSwitcher.Services.Keyboard
{
    public class HotKeyInterceptor : IKeyboardInterceptor
    {
        private const int WM_HOTKEY = 0x0312;
        const int MOD_CONTROL = 0x0002;
        const int MOD_WIN = 0x0008;
        const int VK_TAB = 0x09;

        private readonly Subject<Unit> _signalSubject = new Subject<Unit>();
        private readonly int _hotkeyId = 213769420;
        private readonly MessageWindow _messageWindow;

        public IObservable<Unit> Signal => _signalSubject.AsObservable();

        [RequiresAssemblyFiles()]
        public HotKeyInterceptor()
        {
            _messageWindow = new MessageWindow();
            _messageWindow.HotKeyPressed += OnHotKeyPressed;

            UnregisterHotKey(_messageWindow.Handle, _hotkeyId);
            
            if (!RegisterHotKey(_messageWindow.Handle, _hotkeyId, MOD_WIN | MOD_CONTROL, VK_TAB))
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = GetErrorMessage(error);
                throw new Win32Exception(error, $"Could not register the hot key. {errorMessage}");
            }
        }

        private void OnHotKeyPressed(object? sender, EventArgs e)
        {
            _signalSubject.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            UnregisterHotKey(_messageWindow.Handle, _hotkeyId);
            _messageWindow.Dispose();
            _signalSubject.Dispose();
        }
        
        
        private string GetErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 1409:
                    return "The hotkey is already registered by another application.";
                case 1400:
                    return "The window handle is not valid.";
                case 87:
                    return "An invalid parameter was passed to the function.";
                default:
                    return $"Unknown error occurred. Error code: {errorCode}";
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class MessageWindow : IDisposable
        {
            private const int WS_EX_TOOLWINDOW = 0x80;
            private const int WS_POPUP = unchecked((int)0x80000000);

            public event EventHandler? HotKeyPressed;

            private readonly IntPtr _hwnd;

            public IntPtr Handle => _hwnd;

            [RequiresAssemblyFiles("Calls System.Runtime.InteropServices.Marshal.GetHINSTANCE(Module)")]
            public MessageWindow()
            {
                var wndClass = new WNDCLASS
                {
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProc),
                    hInstance = Marshal.GetHINSTANCE(typeof(MessageWindow).Module),
                    lpszClassName = "MessageWindowClass"
                };

                var classAtom = RegisterClass(ref wndClass);
                if (classAtom == 0)
                    throw new InvalidOperationException("Failed to register window class");

                _hwnd = CreateWindowEx(
                    WS_EX_TOOLWINDOW,
                    classAtom,
                    "MessageWindow",
                    WS_POPUP,
                    0, 0, 0, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    wndClass.hInstance,
                    IntPtr.Zero);

                if (_hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create message window");
            }

            private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                if (msg == WM_HOTKEY)
                {
                    HotKeyPressed?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                }
                return DefWindowProc(hwnd, msg, wParam, lParam);
            }

            public void Dispose()
            {
                if (_hwnd != IntPtr.Zero)
                {
                    DestroyWindow(_hwnd);
                }
            }

            [DllImport("user32.dll")]
            private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

            [DllImport("user32.dll")]
            private static extern IntPtr CreateWindowEx(
                int dwExStyle,
                ushort classAtom,
                string lpWindowName,
                int dwStyle,
                int x, int y,
                int nWidth, int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam);

            [DllImport("user32.dll")]
            private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DestroyWindow(IntPtr hwnd);

            [StructLayout(LayoutKind.Sequential)]
            private struct WNDCLASS
            {
                public int style;
                public IntPtr lpfnWndProc;
                public int cbClsExtra;
                public int cbWndExtra;
                public IntPtr hInstance;
                public IntPtr hIcon;
                public IntPtr hCursor;
                public IntPtr hbrBackground;
                [MarshalAs(UnmanagedType.LPStr)]
                public string lpszMenuName;
                [MarshalAs(UnmanagedType.LPStr)]
                public string lpszClassName;
            }
        }
    }
}