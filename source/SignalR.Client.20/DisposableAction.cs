using System;

namespace SignalR.Client._20
{
    internal class DisposableAction : IDisposable
    {
        private readonly System.Action _action;
        public DisposableAction(System.Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
