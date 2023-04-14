namespace Backhand.Protocols.Dlp
{
    public class DlpCommandErrorException : DlpException
    {
        public DlpErrorCode ErrorCode { get; }

        public DlpCommandErrorException(DlpErrorCode errorCode) : base($"DLP command error: {errorCode}")
        {
            ErrorCode = errorCode;
        }
    }
}
