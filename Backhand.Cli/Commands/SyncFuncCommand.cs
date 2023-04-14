using Backhand.Dlp;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands
{
    public abstract class SyncFuncCommand : Command
    {
        protected readonly Option<string> SerialPortNameOption = new(new[] { "--port", "-p" })
        {
            IsRequired = true
        };

        public SyncFuncCommand(string name, string description) : base(name, description)
        {
            AddOption(SerialPortNameOption);
        }

        protected async Task RunDlpServers(string serialPortName, DlpSyncFunc syncFunc, CancellationToken cancellationToken)
        {
            SerialDlpServer dlpServer = new SerialDlpServer(syncFunc, serialPortName, true, cancellationToken);
            await dlpServer.RunAsync();
        }
    }
}
