using LibUsbDotNet;

namespace Backhand.Usb
{
    public sealed class UsbDeviceConnection : IDisposable
    {
        public UsbDeviceDescriptor DeviceDescriptor { get; }
        internal UsbDevice UsbDevice { get; }
        public string DevicePath => UsbDevice.DevicePath;

        internal UsbDeviceConnection(UsbDeviceDescriptor deviceDescriptor, UsbDevice usbDevice)
        {
            DeviceDescriptor = deviceDescriptor;
            UsbDevice = usbDevice;
        }

        public static UsbDeviceConnection Open(UsbDeviceDescriptor deviceDescriptor)
        {
            bool opened = deviceDescriptor.UsbRegistry.Open(out UsbDevice usbDevice);

            if (!opened)
            {
                throw new UsbException("Failed to open device");
            }

            return new UsbDeviceConnection(deviceDescriptor, usbDevice);
        }

        public void Dispose()
        {
            UsbDevice.Close();
        }
    }
}