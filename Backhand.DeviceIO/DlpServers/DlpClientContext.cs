using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpClientContext
    {
        public DlpConnection Connection { get; }

        public DlpClientContext(DlpConnection connection)
        {
            Connection = connection;
        }
    }
}
