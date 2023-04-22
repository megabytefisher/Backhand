using System;

namespace Backhand.Common
{
    public sealed class DisposableCallback : IDisposable
    {
        private readonly Action _callback;
        private bool _disposed;

        public DisposableCallback(Action callback)
        {
            _callback = callback;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _callback();
        }
    }
}
