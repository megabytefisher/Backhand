using System.Buffers;
using Backhand.Common.BinarySerialization;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public static class UsbHandshake
    {
        [BinarySerializable]
        private class GetExtConnectionInfoResponse
        {
            [BinarySerialize]
            public byte PortCount
            {
                get => (byte)Ports.Length;
                set => Ports = Enumerable.Range(1, value).Select(_ => new ExtConnectionPortInfo()).ToArray();
            }

            [BinarySerialize]
            public bool HasDifferentEndpoints { get; set; }

            [BinarySerialize]
            public byte[] Padding {get; set; } = new byte[2];

            [BinarySerialize]
            public ExtConnectionPortInfo[] Ports { get; private set; } = Array.Empty<ExtConnectionPortInfo>();
        }

        [BinarySerializable]
        private class ExtConnectionPortInfo
        {
            [BinarySerialize(Length = 4)]
            public string Type { get; set; } = string.Empty;

            [BinarySerialize]
            public byte PortNumber { get; set; }

            [BinarySerialize]
            public byte Endpoints { get; set; }

            [BinarySerialize]
            public byte[] Padding { get; set; } = new byte[2];

            public byte InEndpoint => (byte)((Endpoints & ExtConnectionInEndpointBitmask) >> ExtConnectionInEndpointShift);
            public byte OutEndpoint => (byte)((Endpoints & ExtConnectionOutEndpointBitmask) >> ExtConnectionOutEndpointShift);

            private const int ExtConnectionInEndpointBitmask = 0b11110000;
            private const int ExtConnectionInEndpointShift = 4;
            private const int ExtConnectionOutEndpointBitmask = 0b00001111;
            private const int ExtConnectionOutEndpointShift = 0;

            public ExtConnectionPortInfo()
            {
            }
        }
        
        // Device-to-host, Vendor, Endpoint
        private const byte UsbControlTransferRequestType = 0xC2;

        public static (ReadEndpointID, WriteEndpointID) DoHandshake(UsbDeviceConnection deviceConnection, UsbHandshakeMode mode)
        {
            return mode switch
            {
                UsbHandshakeMode.None => DoNoneHandshake(deviceConnection),
                UsbHandshakeMode.New => DoNewHandshake(deviceConnection),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private static (ReadEndpointID, WriteEndpointID) DoNoneHandshake(UsbDeviceConnection deviceConnection)
        {
            return (ReadEndpointID.Ep02, WriteEndpointID.Ep02);
        }

        private static (ReadEndpointID, WriteEndpointID) DoNewHandshake(UsbDeviceConnection deviceConnection)
        {
            UsbSetupPacket setupPacket = new(UsbControlTransferRequestType, 0x04, 0, 0, 20);

            byte[] responseBuffer = new byte[20];
            bool success = deviceConnection.UsbDevice.ControlTransfer(ref setupPacket, responseBuffer, responseBuffer.Length, out int len);

            if (!success)
            {
                throw new UsbException("Failed to get extended connection info");
            }

            if (len != responseBuffer.Length)
            {
                throw new UsbException("Extended connection info result had unexpected length");
            }


            GetExtConnectionInfoResponse response = new();
            BinarySerializer<GetExtConnectionInfoResponse>.Deserialize(new ReadOnlySequence<byte>(responseBuffer), response);

            ExtConnectionPortInfo? syncPort = response.Ports.FirstOrDefault(p => p.Type == "cnys");
            if (syncPort == null)
            {
                throw new UsbException("No sync port found");
            }

            ReadEndpointID readEndpoint = (ReadEndpointID)(syncPort.InEndpoint | 0b10000000);
            WriteEndpointID writeEndpoint = (WriteEndpointID)syncPort.OutEndpoint;

            return (readEndpoint, writeEndpoint);
        }
    }
}