using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Backhand.Usb
{
    public class UsbDeviceConnection
    {
        public string DevicePath => UsbDevice.DevicePath;

        internal UsbDevice UsbDevice { get; }

        internal UsbDeviceConnection(UsbDevice usbDevice)
        {
            UsbDevice = usbDevice;
        }

        public void Dispose()
        {
            UsbDevice?.Close();
        }
    }
}