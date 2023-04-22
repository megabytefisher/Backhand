using Backhand.Common;
using Backhand.Common.Threading;
using LibUsbDotNet.Main;

namespace Backhand.Usb.Internal;

public static class UsbTransferExtensions
{
    public static async Task<(ErrorCode, int)> WaitAsync(this UsbTransfer transfer, CancellationToken cancellationToken = default)
    {
        // When the given cancellation token is cancelled, cancel the transfer.
        await using var transferCancellation =
            cancellationToken.Register(() => transfer.Cancel()).ConfigureAwait(false);

        // Create an inner cancellation token source for the wait handles.
        using CancellationTokenSource waitCancellationSource = new();
        
        // Create tasks that wait for the transfer to complete or be cancelled.
        Task transferCompleteWaitTask = transfer.AsyncWaitHandle.ToTask(waitCancellationSource.Token);
        Task transferCancelledWaitTask = transfer.CancelWaitHandle.ToTask(waitCancellationSource.Token);
        
        // Ensure that wait handle tasks will not be leaked.
        await using var transferCompleteWaitCancellation =
            AsyncDisposableCallback.EnsureCompletion(transferCompleteWaitTask, waitCancellationSource).ConfigureAwait(false);
        await using var transferCancelledWaitCancellation =
            AsyncDisposableCallback.EnsureCompletion(transferCancelledWaitTask, waitCancellationSource).ConfigureAwait(false);
        
        // Wait for either the transfer to complete or be cancelled.
        Task transferTask = await Task.WhenAny(transferCompleteWaitTask, transferCancelledWaitTask).ConfigureAwait(false);
        
        // If the transfer was cancelled, throw an exception.
        if (transferTask == transferCancelledWaitTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Transfer cancelled");
        }
        
        // Get the result of the transfer.
        // This Wait should be synchronous as the transfer has already completed.
        ErrorCode errorCode = transfer.Wait(out int bytesTransferred);
        return (errorCode, bytesTransferred);
    }
}