using System;

namespace Backhand.DeviceIO.Usb
{
    public class UsbDeviceException : Exception
    {
        public UsbDeviceException(string message)
            : base(message)
        {
        }
    }
}
