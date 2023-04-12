using System;

namespace Backhand.Protocols.Slp
{
    public class SlpException : Exception
    {
        public SlpException(string message)
            : base(message)
        {
        }
    }
}
