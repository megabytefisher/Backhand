using System;
using System.Collections.Generic;

namespace Backhand.Protocols.Dlp
{
    public class DlpCommandDefinition
    {
        public required byte Opcode { get; init; }
        public DlpArgumentDefinition[] RequestArguments { get; init; } = Array.Empty<DlpArgumentDefinition>();
        public DlpArgumentDefinition[] ResponseArguments { get; init; } = Array.Empty<DlpArgumentDefinition>();
    }
}
