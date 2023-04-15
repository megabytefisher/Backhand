using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public class UsbDeviceDescriptor
    {
        public string DevicePath { get; }

        internal UsbDeviceDescriptor(string devicePath)
        {
            DevicePath = devicePath;
        }

        public UsbDeviceConnection Open()
        {
            return new UsbDeviceConnection(UsbDevice.OpenUsbDevice(d => d.DevicePath == DevicePath));
        }
    }
}