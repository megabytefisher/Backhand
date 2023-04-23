using System.Buffers;
using Backhand.Common.BinarySerialization;
using Backhand.Usb.Internal.ControlTransfers;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public static class UsbHandshake
    {
        // Device-to-host, Vendor, Endpoint
        private const byte UsbControlTransferRequestType = 0xC2;

        public static (ReadEndpointID, WriteEndpointID) DoHandshake(UsbDeviceConnection deviceConnection, UsbHandshakeMode mode)
        {
            return mode switch
            {
                UsbHandshakeMode.None => DoNoneHandshake(),
                UsbHandshakeMode.New => DoNewHandshake(deviceConnection),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private static (ReadEndpointID, WriteEndpointID) DoNoneHandshake()
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