using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Common.Threading
{
    public static class WaitHandleExtensions
    {
        public static Task ToTask(this WaitHandle waitHandle, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource tcs = new();

            object callbackHandleInitLock = new();
            lock (callbackHandleInitLock)
            {
                RegisteredWaitHandle? callbackHandle = null;
                RegisteredWaitHandle? cancellationHandle = null;
                
                callbackHandle = ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    (_, _) =>
                    {
                        tcs.TrySetResult();

                        lock (callbackHandleInitLock)
                        {
                            callbackHandle!.Unregister(null);
                            cancellationHandle?.Unregister(null);
                        }
                    },
                    null,
                    Timeout.Infinite,
                    true);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationHandle = ThreadPool.RegisterWaitForSingleObject(
                        cancellationToken.WaitHandle,
                        (_, _) =>
                        {
                            tcs.TrySetCanceled();

                            lock (callbackHandleInitLock)
                            {
                                callbackHandle.Unregister(null);
                                cancellationHandle!.Unregister(null);
                            }
                        },
                        null,
                        Timeout.Infinite,
                        true);
                }
            }

            return tcs.Task;
        }
    }
}
