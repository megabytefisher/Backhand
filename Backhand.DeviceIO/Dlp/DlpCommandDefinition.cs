namespace Backhand.DeviceIO.Dlp
{
    public class DlpCommandDefinition
    {
        public DlpOpcode Opcode { get; }
        public DlpArgumentDefinition[] RequestArguments { get; }
        public DlpArgumentDefinition[] ResponseArguments { get; }

        public DlpCommandDefinition(DlpOpcode opcode, DlpArgumentDefinition[] requestArguments, DlpArgumentDefinition[] responseArguments)
        {
            Opcode = opcode;
            RequestArguments = requestArguments;
            ResponseArguments = responseArguments;
        }
    }
}
