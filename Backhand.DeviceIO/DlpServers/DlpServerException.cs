using System;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpServerException : Exception
    {
        public DlpServerException(string message)
            : base(message)
        {
        }
    }
}