using System.Collections.Generic;
using System.Linq;

namespace Backhand.DeviceIO.Usb
{
    public static class UsbDeviceConfigs
    {
        public static readonly IReadOnlyList<UsbDeviceConfig> AllConfigs = new List<UsbDeviceConfig>()
        {
            new(0x081E, 0xDF00, "AlphaSmart Dana", UsbDeviceHandshakeType.New),
            new(0x082D, 0x0100, "HandSpring Visor", UsbDeviceHandshakeType.None, UsbProtocolType.Slp)
        };
        
        public static readonly IReadOnlyDictionary<(ushort vendorId, ushort productId), UsbDeviceConfig> Devices = AllConfigs
            .ToDictionary((c) => (c.VendorId, c.ProductId));
    }
}
