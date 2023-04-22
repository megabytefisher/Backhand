using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public class UsbDeviceDescriptor
    {
        internal UsbRegistry UsbRegistry { get; }
        public string DevicePath => UsbRegistry.DevicePath;

        internal UsbDeviceDescriptor(UsbRegistry usbRegistry)
        {
            UsbRegistry = usbRegistry;
        }
    }
}