using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Protocols.Cmp
{
    public class CmpException : Exception
    {
        public CmpException(string message)
            : base(message)
        {
        }
    }
}
