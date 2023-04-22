using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public static class UsbDeviceConfigs
    {
        public static readonly IReadOnlyList<UsbDeviceConfig> AllConfigs = new List<UsbDeviceConfig>()
        {
            new(0x081E, 0xDF00, "AlphaSmart Dana", UsbHandshakeMode.New),
            new(0x082D, 0x0100, "HandSpring Visor", UsbHandshakeMode.None, UsbProtocolType.Slp)
        };
        
        public static readonly IReadOnlyDictionary<(ushort vendorId, ushort productId), UsbDeviceConfig> Devices = AllConfigs
            .ToDictionary((c) => (c.VendorId, c.ProductId));

        public static ICollection<(UsbDeviceDescriptor, UsbDeviceConfig)> GetAvailableDevices()
        {
            var compatibleDevices = new List<(UsbDeviceDescriptor, UsbDeviceConfig)>();
            foreach (UsbRegistry usbDevice in UsbDevice.AllDevices)
            {
                if (Devices.TryGetValue((Convert.ToUInt16(usbDevice.Vid), Convert.ToUInt16(usbDevice.Pid)), out var config))
                {
                    compatibleDevices.Add((new UsbDeviceDescriptor(usbDevice), config));
                }
            }

            return compatibleDevices;
        }
    }
}