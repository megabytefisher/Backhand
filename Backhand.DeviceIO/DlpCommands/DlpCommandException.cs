using System;

namespace Backhand.DeviceIO.DlpCommands
{
    public class DlpCommandException : Exception
    {
        public DlpCommandException(string message)
            : base(message)
        {
        }
    }
}
