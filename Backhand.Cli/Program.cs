using Backhand.DeviceIO.Cmp;
using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Slp;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using SlpDevice slp = new SlpDevice("COM4");

            slp.ReceivedPacket += (s, e) =>
            {
                Console.WriteLine("RECV");
                Console.WriteLine(e.Packet);
                Console.WriteLine();
            };

            slp.SendingPacket += (s, e) =>
            {
                Console.WriteLine("SEND");
                Console.WriteLine(e.Packet);
                Console.WriteLine();
            };

            using PadpConnection padp = new PadpConnection(slp, 3, 3, 0xff);

            CmpConnection cmp = new CmpConnection(padp);

            Task ioTask = slp.RunIOAsync();

            await cmp.DoHandshakeAsync();

            await ioTask;
        }
    }
}