using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Utility
{
    public class AsyncFlag : IDisposable
    {
        private SemaphoreSlim _semaphore;

        public AsyncFlag()
        {
            _semaphore = new SemaphoreSlim(0, 1);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        public void Set()
        {
            _semaphore.Release();
        }

        public Task WaitAsync()
        {
            return _semaphore.WaitAsync();
        }
    }
}
