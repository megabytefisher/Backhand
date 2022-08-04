using Backhand.DeviceIO.NetSync;
using Backhand.DeviceIO.Utility;
using MadWizard.WinUSBNet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb.Windows
{
    public class WindowsUsbNetSyncDevice : NetSyncDevice
    {
        private class GetExtConnectionInfoResponse
        {
            public byte PortCount { get; set; }
            public bool HasDifferentEndpoints { get; set; }
            public ExtConnectionPortInfo[] Ports { get; set; }
        }

        private class ExtConnectionPortInfo
        {
            public string Type { get; set; }
            public byte PortNumber { get; set; }
            public byte InEndpoint { get; set; }
            public byte OutEndpoint { get; set; }
        }

        private USBDevice _usbDevice;

        private byte? _inEndpoint;
        private byte? _outEndpoint;

        // Device-to-host, Vendor, Endpoint
        private const byte UsbControlTransferRequestType = 0xC2;

        private const int GetExtConnectionInfoResponseLength = 20;
        private const int ExtConnectionInEndpointBitmask = 0b11110000;
        private const int ExtConnectionInEndpointShift = 4;
        private const int ExtConnectionOutEndpointBitmask = 0b00001111;
        private const int ExtConnectionOutEndpointShift = 0;

        private readonly Guid WinUsbGuid = new Guid("dee824ef-729b-4a0e-9c14-b7117d33a817");

        public WindowsUsbNetSyncDevice(USBDeviceInfo usbDeviceInfo)
        {
            _usbDevice = new USBDevice(usbDeviceInfo); 
        }

        public override async Task DoHandshake()
        {
            // TODO: convert Control methods to async..
            byte[] result = new byte[20];
            int len = _usbDevice.ControlIn(UsbControlTransferRequestType, 0x04, 0x00, 0x00, result);

            GetExtConnectionInfoResponse response = ReadGetExtConnectionInfoResponse(new ReadOnlySequence<byte>(result, 0, len));

            ExtConnectionPortInfo? syncPort = response.Ports.FirstOrDefault(p => p.Type == "cnys");
            if (syncPort == null)
                throw new NetSyncException("Couldn't find correct USB device port");

            _inEndpoint = (byte)(syncPort.InEndpoint | 0b10000000);
            _outEndpoint = syncPort.OutEndpoint;

            await base.DoHandshake().ConfigureAwait(false);
        }

        public async Task RunIOAsync(CancellationToken cancellationToken = default)
        {
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken.None))
            {
                Task readerTask = RunReaderAsync(linkedCts.Token);
                //Task writerTask = RunWriterAsync(linkedCts.Token);

                try
                {
                    await Task.WhenAny(readerTask).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EX");
                }
            }

            //_workerExited.Set();
        }

        private Task RunReaderAsync(CancellationToken cancellationToken)
        {
            Pipe readerPipe = new Pipe();
            return Task.WhenAll(
                FillReadPipeAsync(readerPipe.Writer, cancellationToken),
                ProcessReadPipeAsync(readerPipe.Reader, cancellationToken));
        }

        private async Task FillReadPipeAsync(PipeWriter writer, CancellationToken cancellationToken)
        {
            const int minimumBufferSize = 256;

            USBPipe usbPipe = _usbDevice.Interfaces.First().Pipes[_inEndpoint.Value];

            while (!cancellationToken.IsCancellationRequested)
            {
                //Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                byte[] readBuffer = ArrayPool<byte>.Shared.Rent(minimumBufferSize);

                int bytesRead = await Task.Run(() => usbPipe.Read(readBuffer)).ConfigureAwait(false);
                Memory<byte> pipeBuffer = writer.GetMemory(bytesRead);
                ((Span<byte>)readBuffer).Slice(0, bytesRead).CopyTo(pipeBuffer.Span);
                writer.Advance(bytesRead);

                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                    break;
            }

            await writer.CompleteAsync();
        }

        private async Task ProcessReadPipeAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition processedPosition = ReadPackets(buffer);

                reader.AdvanceTo(processedPosition, buffer.End);

                if (result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync();
        }

        /*private async Task RunWriterAsync(CancellationToken cancellationToken)
        {
            PipeWriter serialPortWriter = PipeWriter.Create(_serialPort.BaseStream);

            while (!cancellationToken.IsCancellationRequested)
            {
                SlpSendJob sendJob = await _sendQueue.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                Memory<byte> sendBuffer = serialPortWriter.GetMemory(sendJob.Length);
                sendJob.Buffer.CopyTo(sendBuffer);

                serialPortWriter.Advance(sendJob.Length);
                await serialPortWriter.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Dispose sendJob to return allocated array to pool
                sendJob.Dispose();
            }
        }*/

        public override void SendPacket(NetSyncPacket packet)
        {
            
        }

        private GetExtConnectionInfoResponse ReadGetExtConnectionInfoResponse(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);

            GetExtConnectionInfoResponse result = new GetExtConnectionInfoResponse();
            result.PortCount = bufferReader.Read();
            result.HasDifferentEndpoints = bufferReader.Read() != 0;
            bufferReader.Advance(2); // padding
            result.Ports = new ExtConnectionPortInfo[result.PortCount];
            for (int i = 0; i < result.PortCount; i++)
            {
                result.Ports[i] = ReadExtConnectionPortInfo(ref bufferReader);
            }

            return result;
        }

        private ExtConnectionPortInfo ReadExtConnectionPortInfo(ref SequenceReader<byte> bufferReader)
        {
            ExtConnectionPortInfo result = new ExtConnectionPortInfo();
            result.Type = Encoding.ASCII.GetString(bufferReader.Sequence.Slice(bufferReader.Position, 4));
            bufferReader.Advance(4);

            result.PortNumber = bufferReader.Read();

            byte endpoints = bufferReader.Read();
            result.InEndpoint = (byte)((endpoints & ExtConnectionInEndpointBitmask) >> ExtConnectionInEndpointShift);
            result.OutEndpoint = (byte)((endpoints & ExtConnectionOutEndpointBitmask) >> ExtConnectionOutEndpointShift);

            bufferReader.Advance(2); // padding

            return result;
        }
    }
}
