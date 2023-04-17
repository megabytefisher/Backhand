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
            Stream steam = client.GetStream();
            Input = PipeReader.Create(client.GetStream());
            Output = PipeWriter.Create(client.GetStream());
        }
    }
}
