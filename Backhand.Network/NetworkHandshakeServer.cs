using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Network
{
    public class NetworkHandshakeServer : IDisposable
    {
        private readonly Socket _socket;
        private readonly int _port;

        public NetworkHandshakeServer(int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _port = port;
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var buffer = new byte[22];

                SocketReceiveMessageFromResult result = await _socket.ReceiveMessageFromAsync(buffer, new IPEndPoint(IPAddress.Any, 0), cancellationToken);

                if (result.ReceivedBytes != 22)
                {
                    throw new Exception("Unexpected handshake packet");
                }

                buffer[2] = 2;

                await _socket.SendToAsync(buffer, result.RemoteEndPoint);

                Console.WriteLine($"Received message ({result.ReceivedBytes}) from {result.RemoteEndPoint}");
            }
        }
    }
}
