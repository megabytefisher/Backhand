using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Common;

public sealed class AsyncDisposableCallback : IAsyncDisposable
{
    private readonly Func<Task> _callback;
    private bool _disposed;
    
    public AsyncDisposableCallback(Func<Task> callback)
    {
        _callback = callback;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        _disposed = true;
        await _callback().ConfigureAwait(false);
    }

    public static IAsyncDisposable EnsureCompletion(Task task, CancellationTokenSource? cts)
    {
        return new AsyncDisposableCallback(async () =>
        {
            cts?.Cancel();
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Swallow.
            }
        });
    }

    public static IAsyncDisposable EnsureCompletion(IEnumerable<Task> tasks, CancellationTokenSource? cts)
    {
        return new AsyncDisposableCallback(async () =>
        {
            cts?.Cancel();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // Swallow.
            }
        });
    }
}