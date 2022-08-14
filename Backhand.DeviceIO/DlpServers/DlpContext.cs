using Backhand.DeviceIO.Dlp;

namespace Backhand.DeviceIO.DlpServers
{
    public class DlpContext
    {
        public DlpConnection Connection { get; }

        public DlpContext(DlpConnection connection)
        {
            Connection = connection;
        }
    }
}
