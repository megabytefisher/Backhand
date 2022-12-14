using Backhand.Utility.Buffers;
using LibUsbDotNet.Main;
using LibUsbDotNet;
using System;
using System.Buffers;
using System.Linq;
using System.Text;

namespace Backhand.DeviceIO.Usb
{
    public static class UsbHandshake
    {
        private class GetExtConnectionInfoResponse
        {
            public byte PortCount { get; set; }
            public bool HasDifferentEndpoints { get; set; }
            public ExtConnectionPortInfo[] Ports { get; set; } = Array.Empty<ExtConnectionPortInfo>();
        }

        private class ExtConnectionPortInfo
        {
            public string Type { get; set; } = "";
            public byte PortNumber { get; set; }
            public byte InEndpoint { get; set; }
            public byte OutEndpoint { get; set; }
        }

        // Device-to-host, Vendor, Endpoint
        private const byte UsbControlTransferRequestType = 0xC2;

        private const int GetExtConnectionInfoResponseLength = 20;
        private const int ExtConnectionInEndpointBitmask = 0b11110000;
        private const int ExtConnectionInEndpointShift = 4;
        private const int ExtConnectionOutEndpointBitmask = 0b00001111;
        private const int ExtConnectionOutEndpointShift = 0;

        public static (ReadEndpointID readEndpoint, WriteEndpointID writeEndpoint) DoHardwareHandshake(UsbDevice usbDevice, UsbDeviceHandshakeType handshakeType)
        {
            // TODO: convert Control methods to async..
            if (handshakeType == UsbDeviceHandshakeType.New)
            {
                byte[] result = new byte[20];
                UsbSetupPacket setupPacket = new(UsbControlTransferRequestType, 0x04, 0, 0, 20);
                bool success = usbDevice.ControlTransfer(ref setupPacket, result, 20, out int len);
                if (!success)
                    throw new UsbDeviceException("Failed to do USB control transfer");
                if (len != 20)
                    throw new UsbDeviceException("Didn't get expected length from USB control transfer");

                GetExtConnectionInfoResponse response = ReadGetExtConnectionInfoResponse(new ReadOnlySequence<byte>(result, 0, len));

                ExtConnectionPortInfo? syncPort = response.Ports.FirstOrDefault(p => p.Type == "cnys");
                if (syncPort == null)
                    throw new UsbDeviceException("Couldn't find correct USB device port");

                ReadEndpointID inEndpoint = (ReadEndpointID)(syncPort.InEndpoint | 0b10000000);
                WriteEndpointID outEndpoint = (WriteEndpointID)syncPort.OutEndpoint;

                return (inEndpoint, outEndpoint);
            }
            else
            {
                ReadEndpointID inEndpoint = ReadEndpointID.Ep02;
                WriteEndpointID outEndpoint = WriteEndpointID.Ep02;

                return (inEndpoint, outEndpoint);
            }
        }

        private static GetExtConnectionInfoResponse ReadGetExtConnectionInfoResponse(ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);

            GetExtConnectionInfoResponse result = new();
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

        private static ExtConnectionPortInfo ReadExtConnectionPortInfo(ref SequenceReader<byte> bufferReader)
        {
            ExtConnectionPortInfo result = new();
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
