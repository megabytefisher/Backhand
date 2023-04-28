using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Common.Buffers;
using Backhand.PalmDb;
using Backhand.PalmDb.Memory;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Backhand.Cli.Commands.DbCommands
{
    /*public class AssembleCommand : Command
    {
        public readonly Argument<DirectoryInfo> InputArgument =
            new("input", "The input directory to assemble");

        public AssembleCommand()
            : base("assemble", "Assembles a Palm database file from its constituent parts")
        {
            AddArgument(InputArgument);

            this.SetHandler(async (context) =>
            {
                IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();

                DirectoryInfo input = context.ParseResult.GetValueForArgument(InputArgument);

                CancellationToken cancellationToken = context.GetCancellationToken();

                await AssembleFileAsync(input, console, cancellationToken);
            });
        }

        private static async Task AssembleFileAsync(DirectoryInfo inputDirectory, IAnsiConsole console, CancellationToken cancellationToken)
        {
            FileInfo headerFile = new(Path.Combine(inputDirectory.FullName, "header.json"));
            if (!headerFile.Exists)
            {
                throw new FileNotFoundException("The header file was not found", headerFile.FullName);
            }

            await using FileStream headerInfoStream = headerFile.OpenRead();
            DatabaseHeaderInfo headerInfo = (await JsonSerializer.DeserializeAsync<DatabaseHeaderInfo>(headerInfoStream))!;
            await headerInfoStream.DisposeAsync();

            Database database = headerInfo.Attributes.HasFlag(DatabaseAttributes.ResourceDb)
                ? new ResourceDatabase()
                : new RecordDatabase();

            database.Name = headerInfo.Name;
            database.Attributes = headerInfo.Attributes;
            database.CreationDate = headerInfo.CreationDate;
            database.ModificationDate = headerInfo.ModificationDate;
            database.LastBackupDate = headerInfo.LastBackupDate;
            database.ModificationNumber = headerInfo.ModificationNumber;
            database.Type = headerInfo.Type;
            database.Creator = headerInfo.Creator;
            database.UniqueIdSeed = headerInfo.UniqueIdSeed;

            FileInfo appInfoFile = new(Path.Combine(inputDirectory.FullName, "appinfo.bin"));
            if (appInfoFile.Exists)
            {
                database.AppInfo = new byte[appInfoFile.Length];
                await using FileStream appInfoStream = appInfoFile.OpenRead();
                await appInfoStream.FillBufferAsync(database.AppInfo, cancellationToken);
            }

            FileInfo sortInfoFile = new(Path.Combine(inputDirectory.FullName, "sortinfo.bin"));
            if (sortInfoFile.Exists)
            {
                database.SortInfo = new byte[sortInfoFile.Length];
                await using FileStream sortInfoStream = sortInfoFile.OpenRead();
                await sortInfoStream.FillBufferAsync(database.SortInfo, cancellationToken);
            }

            if (database is RecordDatabase recordDatabase)
            {
                outputFile = outputFile ?? new FileInfo(Path.ChangeExtension(inputDirectory.Name, ".pdb"));
                await AssembleRecordDatabaseAsync(recordDatabase, inputDirectory, console, cancellationToken);
            }
            else if (database is ResourceDatabase resourceDatabase)
            {
                outputFile = outputFile ?? new FileInfo(Path.ChangeExtension(inputDirectory.Name, ".prc"));
                await AssembleResourceDatabaseAsync(resourceDatabase, inputDirectory, console, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException();
            }

            await using FileStream outputStream = outputFile.OpenWrite();
            await database.SerializeAsync(outputStream, cancellationToken);
        }

        private static async Task AssembleRecordDatabaseAsync(RecordDatabase database, DirectoryInfo inputDirectory, IConsole console, CancellationToken cancellationToken)
        {
            foreach (DirectoryInfo recordDirectory in inputDirectory.EnumerateDirectories())
            {
                FileInfo recordInfoFile = new(Path.Combine(recordDirectory.FullName, "record.json"));
                FileInfo recordDataFile = new(Path.Combine(recordDirectory.FullName, "record.bin"));

                if (!recordInfoFile.Exists || !recordDataFile.Exists)
                {
                    console.WriteLine($"Skipping record directory {recordDirectory.Name} because it is missing a record.json or record.bin file");
                    continue;
                }

                await using FileStream recordInfoStream = recordInfoFile.OpenRead();
                DatabaseRecordInfo recordInfo = (await JsonSerializer.DeserializeAsync<DatabaseRecordInfo>(recordInfoStream))!;
                await recordInfoStream.DisposeAsync();

                RawDatabaseRecord record = new()
                {
                    Attributes = recordInfo.Attributes,
                    Category = recordInfo.Category,
                    Archive = recordInfo.Archive,
                    UniqueId = recordInfo.UniqueId,
                    Data = new byte[recordDataFile.Length]
                };
                
                await using FileStream recordDataStream = recordDataFile.OpenRead();
                await recordDataStream.FillBufferAsync(record.Data, cancellationToken);
                await recordDataStream.DisposeAsync();

                database.Records.Add(record);
            }
        }

        private static async Task AssembleResourceDatabaseAsync(ResourceDatabase database, DirectoryInfo inputDirectory, IConsole console, CancellationToken cancellationToken)
        {
            foreach (DirectoryInfo typeDirectory in inputDirectory.EnumerateDirectories())
            {
                foreach (FileInfo resourceFile in typeDirectory.EnumerateFiles("*.bin"))
                {
                    ushort resourceId;
                    if (!ushort.TryParse(Path.GetFileNameWithoutExtension(resourceFile.Name), out resourceId))
                    {
                        console.WriteLine($"Skipping resource file {resourceFile.Name} because it does not have a valid resource ID");
                        continue;
                    }

                    DatabaseResource resource = new()
                    {
                        Type = typeDirectory.Name,
                        ResourceId = resourceId,
                        Data = new byte[resourceFile.Length]
                    };

                    await using FileStream resourceStream = resourceFile.OpenRead();
                    await resourceStream.FillBufferAsync(resource.Data, cancellationToken);
                    await resourceStream.DisposeAsync();

                    database.Resources.Add(resource);
                }
            }
        }
    }*/
}