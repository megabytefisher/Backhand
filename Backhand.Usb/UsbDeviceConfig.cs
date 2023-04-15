namespace Backhand.Usb
{
    public class UsbDeviceConfig
    {
        public ushort VendorId { get; }
        public ushort ProductId { get; }
        public string Description { get; }
        public UsbHandshakeMode HandshakeMode { get; }
        public UsbProtocolType ProtocolType { get; }

        public UsbDeviceConfig(ushort vendorId, ushort productId, string description, UsbHandshakeMode handhsakeMode, UsbProtocolType protocolType = UsbProtocolType.NetSync)
        {
            VendorId = vendorId;
            ProductId = productId;
            Description = description;
            HandshakeMode = handhsakeMode;
            ProtocolType = protocolType;
        }
    }
}