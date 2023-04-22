using Backhand.Cli.Internal.DatabaseDisassembly;
using Backhand.Pdb;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands.DbCommands
{
    public class DisassembleCommand : Command
    {
        public readonly Argument<FileInfo> InputArgument =
            new("input", "The input file to disassemble");

        public readonly Option<DirectoryInfo?> OutputOption =
            new(new[] { "--output", "-o" }, "The output directory to disassemble to");

        public DisassembleCommand()
            : base("disassemble", "Disassembles a Palm database file into its constituent parts")
        {
            AddArgument(InputArgument);
            AddOption(OutputOption);

            this.SetHandler(async (context) =>
            {
                FileInfo input = context.ParseResult.GetValueForArgument(InputArgument);
                DirectoryInfo? output = context.ParseResult.GetValueForOption(OutputOption);

                CancellationToken cancellationToken = context.GetCancellationToken();

                if (output == null)
                {
                    output = new DirectoryInfo(Path.GetFileNameWithoutExtension(input.Name));
                    output.Create();
                }

                await DisassembleFileAsync(input, output, cancellationToken);
            });
        }

        private static async Task DisassembleFileAsync(FileInfo inputFile, DirectoryInfo outputDirectory, CancellationToken cancellationToken)
        {
            await using FileStream fileStream = inputFile.OpenRead();

            DatabaseHeader header = new();
            await header.DeserializeAsync(fileStream, cancellationToken);

            fileStream.Seek(0, SeekOrigin.Begin);

            Database database = header.Attributes.HasFlag(DatabaseAttributes.ResourceDb)
                ? new ResourceDatabase()
                : new RecordDatabase();
            await database.DeserializeAsync(fileStream, cancellationToken);

            DatabaseHeaderInfo headerInfo = new()
            {
                Name = header.Name,
                Attributes = header.Attributes,
                Version = header.Version,
                CreationDate = header.CreationDate,
                ModificationDate = header.ModificationDate,
                LastBackupDate = header.LastBackupDate,
                ModificationNumber = header.ModificationNumber,
                Type = header.Type,
                Creator = header.Creator,
                UniqueIdSeed = header.UniqueIdSeed
            };

            FileInfo headerFile = new(Path.Combine(outputDirectory.FullName, "header.json"));
            await using FileStream headerFileStream = headerFile.OpenWrite();
            await JsonSerializer.SerializeAsync(headerFileStream, headerInfo, cancellationToken: cancellationToken);
            await headerFileStream.DisposeAsync();

            if (database.AppInfo is { Length: > 0 })
            {
                await using FileStream appInfoFileStream = new(Path.Combine(outputDirectory.FullName, "appinfo.bin"), FileMode.Create);
                await appInfoFileStream.WriteAsync(database.AppInfo, cancellationToken);
            }

            if (database.SortInfo is { Length: > 0 })
            {
                await using FileStream sortInfoFileStream = new(Path.Combine(outputDirectory.FullName, "sortinfo.bin"), FileMode.Create);
                await sortInfoFileStream.WriteAsync(database.SortInfo, cancellationToken);
            }

            if (database is ResourceDatabase resourceDatabase)
            {
                await DisassembleResourceDatabaseAsync(resourceDatabase, outputDirectory, cancellationToken);
            }
            else if (database is RecordDatabase recordDatabase)
            {
                await DisassembleRecordDatabaseAsync(recordDatabase, outputDirectory, cancellationToken);
            }
        }

        private static async Task DisassembleRecordDatabaseAsync(RecordDatabase database, DirectoryInfo outputDirectory, CancellationToken cancellationToken)
        {
            int i = 0;
            foreach (RawDatabaseRecord record in database.Records)
            {
                DatabaseRecordInfo recordInfo = new()
                {
                    Attributes = record.Attributes,
                    Category = record.Category,
                    Archive = record.Archive,
                    UniqueId = record.UniqueId,
                };

                DirectoryInfo recordDirectory = new(Path.Combine(outputDirectory.FullName, $"{i}"));
                recordDirectory.Create();

                FileInfo recordInfoFile = new(Path.Combine(recordDirectory.FullName, "record.json"));
                FileInfo recordDataFile = new(Path.Combine(recordDirectory.FullName, "record.bin"));

                await using FileStream recordInfoFileStream = recordInfoFile.OpenWrite();
                await JsonSerializer.SerializeAsync(recordInfoFileStream, recordInfo, cancellationToken: cancellationToken);
                await recordInfoFileStream.DisposeAsync();

                await using FileStream recordFileStream = recordDataFile.OpenWrite();
                await recordFileStream.WriteAsync(record.Data, cancellationToken);
                await recordFileStream.DisposeAsync();

                i++;
            }
        }

        private static async Task DisassembleResourceDatabaseAsync(ResourceDatabase database, DirectoryInfo outputDirectory, CancellationToken cancellationToken)
        {
            foreach (DatabaseResource resource in database.Resources)
            {
                DirectoryInfo typeDirectory = new(Path.Combine(outputDirectory.FullName, resource.Type));
                typeDirectory.Create();

                FileInfo resourceFile = new(Path.Combine(typeDirectory.FullName, $"{resource.ResourceId}.bin"));

                using FileStream resourceFileStream = resourceFile.OpenWrite();
                await resourceFileStream.WriteAsync(resource.Data, cancellationToken);
                await resourceFileStream.DisposeAsync();
            }
        }
    }
}