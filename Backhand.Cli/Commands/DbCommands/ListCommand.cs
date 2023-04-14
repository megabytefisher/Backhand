using Backhand.Cli.Internal;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp;
using System;
using System.Linq;
using DatabaseMetadata = Backhand.Dlp.Commands.v1_0.Arguments.ReadDbListResponse.DatabaseMetadata;

namespace Backhand.Cli.Commands.DbCommands
{
    public class ListCommand : Command
    {
        public static readonly Option<string> SerialPortNameOption = new(new[] { "--port", "-p" })
        {
            IsRequired = true,
            Arity = ArgumentArity.ExactlyOne
        };

        public ListCommand() : base("list", "Lists databases on a device")
        {
            this.Add(SerialPortNameOption);

            this.SetHandler(
                RunCommandAsync,
                SerialPortNameOption,
                Bind.FromServiceProvider<ILogger>(),
                Bind.FromServiceProvider<IConsole>());
        }

        private async Task RunCommandAsync(string serialPortName, ILogger logger, IConsole console)
        {
            List<DatabaseMetadata> metadataList = new List<DatabaseMetadata>();

            async Task SyncFunc(DlpConnection connection, CancellationToken cancellationToken = default)
            {
                await connection.OpenConduitAsync(cancellationToken);
                ushort startIndex = 0;

                while (true)
                {
                    ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new()
                    {
                        Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                        CardId = 0,
                        StartIndex = startIndex
                    }, cancellationToken);

                    metadataList.AddRange(dbListResponse.Results);
                    startIndex = (ushort)(dbListResponse.LastIndex + 1);

                    if (dbListResponse.LastIndex == dbListResponse.Results.Last().Index)
                    {
                        break;
                    }
                }
            }

            SerialDlpServer dlpServer = new SerialDlpServer(SyncFunc, serialPortName, singleSync: true);
            await dlpServer.RunAsync();

            PrintResults(console, metadataList);
        }

        private void PrintResults(IConsole console, ICollection<DatabaseMetadata> metadataList)
        {
            ConsoleTableColumn<DatabaseMetadata> nameColumn = new()
            {
                Header = "Name",
                GetText = (value) => value.Name
            };

            ConsoleTableColumn<DatabaseMetadata> attributesColumn = new()
            {
                Header = "Attributes",
                GetText = (value) => value.Attributes.ToString()
            };

            console.WriteTable(new[] { nameColumn, attributesColumn }, metadataList);
        }
    }
}
