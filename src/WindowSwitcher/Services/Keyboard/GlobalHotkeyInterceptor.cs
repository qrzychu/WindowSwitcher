// using System;
// using System.Reactive.Subjects;
// using System.Windows.Input;
// using GlobalHotKey;
// using Microsoft.FSharp.Core;
//
// namespace WindowSwitcher.Services;
//
// public class KeyboardShortcutInterceptor : IDisposable
// {
//     private readonly HotKeyManager _hotKeyManager;
//     private readonly Subject<Unit> _signalSubject = new Subject<Unit>();
//     private readonly HotKey _hotKey;
//
//     public IObservable<Unit> Signal => _signalSubject.AsObservable();
//
//     public KeyboardShortcutInterceptor()
//     {
//         _hotKeyManager = new HotKeyManager();
//         _hotKey = new HotKey(Key.S, ModifierKeys.Windows);
//
//         _hotKeyManager.Register(_hotKey);
//         _hotKeyManager.KeyPressed += HotKeyManager_KeyPressed;
//     }
//
//     private void HotKeyManager_KeyPressed(object sender, KeyPressedEventArgs e)
//     {
//         if (e.HotKey.Equals(_hotKey))
//         {
//             _signalSubject.OnNext(Unit.Default);
//         }
//     }
//
//     public void Dispose()
//     {
//         _hotKeyManager.KeyPressed -= HotKeyManager_KeyPressed;
//         _hotKeyManager.Unregister(_hotKey);
//         _hotKeyManager.Dispose();
//         _signalSubject.Dispose();
//     }
// }