using Backhand.Common.Pipelines;
using Backhand.Protocols.Cmp;
using Backhand.Protocols.Dlp;
using Backhand.Protocols.Padp;
using Backhand.Protocols.Slp;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common;

namespace Backhand.Dlp
{
    public class SerialDlpServer : DlpServer
    {
        public string PortName { get; }

        private readonly ILogger _slpLogger;
        private readonly ILogger _padpLogger;
        private readonly ILogger _cmpLogger;
        private readonly ILogger _dlpLogger;

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 57600;

        public SerialDlpServer(string portName, ILoggerFactory? loggerFactory = null)
            : base(loggerFactory)
        {
            PortName = portName;

            _slpLogger = LoggerFactory.CreateLogger(DlpServerLogging.SlpInterfaceCategory);
            _padpLogger = LoggerFactory.CreateLogger(DlpServerLogging.PadpConnectionCategory);
            _cmpLogger = LoggerFactory.CreateLogger(DlpServerLogging.CmpConnectionCategory);
            _dlpLogger = LoggerFactory.CreateLogger(DlpServerLogging.DlpConnectionCategory);
        }

        public override string ToString() => $"serial[{PortName}]";

        public override async Task RunAsync(ISyncHandler syncHandler, bool singleSync, CancellationToken cancellationToken = default)
        {
            Logger.ServerStarting(this);

            Exception? exception = null;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await HandleDeviceAsync(syncHandler, cancellationToken).ConfigureAwait(false);

                        if (singleSync)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO : Something with this exception
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                Logger.ServerStopped(this, exception);
            }
        }

        private async Task HandleDeviceAsync(ISyncHandler syncHandler, CancellationToken cancellationToken)
        {
            string connectionName = SerialDlpConnection.GetConnectionName(PortName);

            await DoCmpPortionAsync(cancellationToken).ConfigureAwait(false);
            Logger.ConnectionOpened(this, connectionName);

            Exception? exception = null;
            try
            {
                await DoDlpPortionAsync(syncHandler, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                Logger.ConnectionClosed(this, connectionName, exception);
            }
        }

        private async Task DoCmpPortionAsync(CancellationToken cancellationToken = default)
        {
            using SerialPort serialPort = new(PortName);
            serialPort.BaudRate = InitialBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            using IDisposable serialPortDispose = new DisposableCallback(serialPort.Close);
            using CancellationTokenSource ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await using SerialPortPipe serialPipe = new(serialPort, cancellationToken: cancellationToken);

            using SlpInterface slpInterface = new SlpInterface(serialPipe, logger: _slpLogger);
            Task slpIoTask = slpInterface.RunIOAsync(ioCts.Token);
            await using ConfiguredAsyncDisposable slpIoDispose =
                AsyncDisposableCallback.EnsureCompletion(slpIoTask, ioCts).ConfigureAwait(false);

            PadpConnection padpConnection = new(slpInterface, 3, 3, logger: _padpLogger);

            CmpConnection cmpConnection = new(padpConnection, logger: _cmpLogger);
            await cmpConnection.WaitForWakeUpAsync(cancellationToken).ConfigureAwait(false);
            await cmpConnection.DoHandshakeAsync(TargetBaudRate, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task DoDlpPortionAsync(ISyncHandler syncHandler, CancellationToken cancellationToken = default)
        {
            using SerialPort serialPort = new(PortName);
            serialPort.BaudRate = TargetBaudRate;
            serialPort.Handshake = Handshake.RequestToSend;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Open();

            using IDisposable serialPortDispose = new DisposableCallback(serialPort.Close);
            using CancellationTokenSource ioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await using SerialPortPipe serialPipe = new(serialPort, cancellationToken: ioCts.Token);

            using SlpInterface slpInterface = new SlpInterface(serialPipe, logger: _slpLogger);
            Task slpIoTask = slpInterface.RunIOAsync(ioCts.Token);
            await using ConfiguredAsyncDisposable slpIoDispose =
                AsyncDisposableCallback.EnsureCompletion(slpIoTask, ioCts).ConfigureAwait(false);

            PadpConnection padpConnection = new(slpInterface, 3, 3, logger: _padpLogger);
            
            // Create DLP connection
            DlpConnection dlpConnection = new SerialDlpConnection(PortName, padpConnection, logger: _dlpLogger);
            await SyncAsync(dlpConnection, syncHandler, cancellationToken).ConfigureAwait(false);
        }

        private class SerialDlpConnection : DlpConnection
        {
            public string RemotePort { get; }

            public SerialDlpConnection(string remotePort, PadpConnection padpConnection, ArrayPool<byte>? arrayPool = null, ILogger? logger = null)
                : base(padpConnection, arrayPool, logger)
            {
                RemotePort = remotePort;
            }

            public static string GetConnectionName(string portName) => $"serial[{portName}]";
            public override string ToString() => GetConnectionName(RemotePort);
        }
    }
}
