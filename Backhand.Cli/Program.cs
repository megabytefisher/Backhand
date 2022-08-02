using Backhand.DeviceIO.Slp;

namespace Backhand.Cli
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SlpDevice slp = new SlpDevice("COM3");
            Task t1 = slp.RunIOAsync();

            await Task.Delay(1000);

            slp.Dispose();

            await Task.Delay(1000);

            await t1.ConfigureAwait(false);
        }
    }
}