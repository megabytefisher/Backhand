using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpCommandDefinition
    {
        public DlpOpcode Opcode { get; private init; }
        public DlpArgumentDefinition[] RequestArguments { get; private init; }
        public DlpArgumentDefinition[] ResponseArguments { get; private init; }

        public DlpCommandDefinition(DlpOpcode opcode, DlpArgumentDefinition[] requestArguments, DlpArgumentDefinition[] responseArguments)
        {
            Opcode = opcode;
            RequestArguments = requestArguments;
            ResponseArguments = responseArguments;
        }
    }
}
