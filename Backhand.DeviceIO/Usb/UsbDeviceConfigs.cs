using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb
{
    public static class UsbDeviceConfigs
    {
        public static readonly IReadOnlyList<UsbDeviceConfig> AllConfigs = new List<UsbDeviceConfig>()
        {
            new UsbDeviceConfig(0x081E, 0xDF00, "AlphaSmart Dana"),
            new UsbDeviceConfig(0x082D, 0x0100, "HandSpring Visor")
        };
        public static readonly IReadOnlyDictionary<(ushort vendorId, ushort productId), UsbDeviceConfig> Devices = AllConfigs
            .ToDictionary((c) => (c.VendorId, c.ProductId));
    }
}
