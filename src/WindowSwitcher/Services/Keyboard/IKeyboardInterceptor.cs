using System;
using System.Reactive;

namespace WindowSwitcher.Services.Keyboard;

public interface IKeyboardInterceptor : IDisposable
{
    IObservable<Unit> Signal { get; }
}