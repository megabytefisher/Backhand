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
        public ushort VendorId { get; }
        public ushort ProductId { get; }
        public string Description { get; }
        public UsbDeviceHandshakeType HandshakeType { get; }
        public UsbProtocolType ProtocolType { get; }

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
