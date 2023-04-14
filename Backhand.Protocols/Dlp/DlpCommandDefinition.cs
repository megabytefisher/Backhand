namespace Backhand.Protocols.Dlp
{
    public class DlpCommandDefinition
    {
        public byte Opcode { get; }
        public DlpArgumentDefinition[] RequestArguments { get; }
        public DlpArgumentDefinition[] ResponseArguments { get; }

        public DlpCommandDefinition(byte opcode, DlpArgumentDefinition[] requestArguments, DlpArgumentDefinition[] responseArguments)
        {
            Opcode = opcode;
            RequestArguments = requestArguments;
            ResponseArguments = responseArguments;
        }
    }
}
