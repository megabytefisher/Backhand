using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

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
