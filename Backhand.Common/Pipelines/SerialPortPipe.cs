using System;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Common.Pipelines
{
    public sealed class SerialPortPipe : IDuplexPipe, IDisposable, IAsyncDisposable
    {
        public SerialPort SerialPort { get; }
        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        private bool _disposePort;
        
        private bool _disposed;
        
        private readonly CancellationTokenRegistration? _cancellationTokenRegistration;

        public SerialPortPipe(SerialPort serialPort, bool disposePort = false, CancellationToken? cancellationToken = null)
        {
            SerialPort = serialPort;
            Input = PipeReader.Create(serialPort.BaseStream);
            Output = PipeWriter.Create(serialPort.BaseStream);
            _disposePort = disposePort;

            _cancellationTokenRegistration = cancellationToken?.Register(() =>
            {
                Input.Complete(new TaskCanceledException());
                Output.Complete(new TaskCanceledException());
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _cancellationTokenRegistration?.Dispose();
            Input.Complete();
            Output.Complete();
            
            if (_disposePort) SerialPort.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            if (_cancellationTokenRegistration != null)
            {
                await _cancellationTokenRegistration.Value.DisposeAsync().ConfigureAwait(false);
            }
            
            await Input.CompleteAsync().ConfigureAwait(false);
            await Output.CompleteAsync().ConfigureAwait(false);
            
            if (_disposePort) SerialPort.Dispose();
        }
    }
}
