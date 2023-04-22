using System.IO.Pipelines;
using System.Net.Sockets;

namespace Backhand.Network
{
    public class NetworkPipe : IDuplexPipe
    {
        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        public NetworkPipe(TcpClient client)
        {
            Stream stream = client.GetStream();
            Input = PipeReader.Create(stream);
            Output = PipeWriter.Create(stream);
        }
    }
}
