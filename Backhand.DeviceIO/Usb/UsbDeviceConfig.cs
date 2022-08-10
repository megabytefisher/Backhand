using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb
{
    public enum UsbDeviceHandshakeType
    {
        None,
        New
    }

    public enum UsbProtocolType
    {
        NetSync,
        Slp
    }

    public class UsbDeviceConfig
    {
        public ushort VendorId { get; private init; }
        public ushort ProductId { get; private init; }
        public string Description { get; private init; }
        public UsbDeviceHandshakeType HandshakeType { get; private init; }
        public UsbProtocolType ProtocolType { get; private init; }

        public UsbDeviceConfig(ushort vendorId, ushort productId, string description, UsbDeviceHandshakeType handshakeType, UsbProtocolType protocolType = UsbProtocolType.NetSync)
        {
            VendorId = vendorId;
            ProductId = productId;
            Description = description;
            HandshakeType = handshakeType;
            ProtocolType = protocolType;
        }
    }
}
