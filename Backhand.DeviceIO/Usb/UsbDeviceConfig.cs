using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Usb
{
    public class UsbDeviceConfig
    {
        public ushort VendorId { get; private init; }
        public ushort ProductId { get; private init; }
        public string Description { get; private init; }

        public UsbDeviceConfig(ushort vendorId, ushort productId, string description)
        {
            VendorId = vendorId;
            ProductId = productId;
            Description = description;
        }
    }
}
