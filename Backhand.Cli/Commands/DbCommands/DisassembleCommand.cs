using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Cli.Internal.DatabaseDisassembly;
using Backhand.PalmDb;
using Backhand.PalmDb.FileIO;
using Backhand.PalmDb.Memory;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Backhand.Cli.Commands.DbCommands
{
    public class DisassembleCommand : Command
    {
        public readonly Argument<FileInfo> InputArgument =
            new("input", "Path to database file to disassemble");

        public DisassembleCommand()
            : base("disassemble", "Disassembles a Palm database file into its constituent parts")
        {
            AddArgument(InputArgument);

            this.SetHandler(async (context) =>
            {
                IAnsiConsole console = context.BindingContext.GetRequiredService<IAnsiConsole>();
                
                FileInfo input = context.ParseResult.GetValueForArgument(InputArgument);

                CancellationToken cancellationToken = context.GetCancellationToken();

                DirectoryInfo outputDirectory = new(Path.GetFileNameWithoutExtension(input.Name));
                outputDirectory.Create();
                
                await DisassembleDatabaseAsync(input, outputDirectory, cancellationToken);
                
                console.MarkupLine($"[green]Disassembled database to {outputDirectory.FullName}[/]");
            });
        }

        private static async Task DisassembleDatabaseAsync(FileInfo inputFile, DirectoryInfo outputDirectory, CancellationToken cancellationToken)
        {
            IPalmDb inputDb = await PalmDbFile.ReadAsync(inputFile, cancellationToken);

            if (inputDb is IPalmRecordDb recordDb)
            {
                await DisassembleDatabaseAsync(recordDb, outputDirectory, cancellationToken);
            }
            
            
        }

        private static async Task DisassembleDatabaseAsync(IPalmRecordDb recordDb, DirectoryInfo outputDirectory, CancellationToken cancellationToken)
        {
            using MemoryStream bufferStream = new();
            
            // Read and output AppInfo
            FileInfo? appInfoFile = null;
            await recordDb.ReadAppInfoAsync(bufferStream, cancellationToken);
            if (bufferStream.Length > 0)
            {
                appInfoFile = new(Path.Combine(outputDirectory.FullName, "_appinfo.bin"));
                await using FileStream appInfoFileStream = appInfoFile.OpenWrite();
                
                bufferStream.Seek(0, SeekOrigin.Begin);
                await bufferStream.CopyToAsync(appInfoFileStream, cancellationToken);
            }
            
            bufferStream.Seek(0, SeekOrigin.Begin);
            bufferStream.SetLength(0);
            
            // Read and output SortInfo
            FileInfo? sortInfoFile = null;
            await recordDb.ReadSortInfoAsync(bufferStream, cancellationToken);
            if (bufferStream.Length > 0)
            {
                sortInfoFile = new(Path.Combine(outputDirectory.FullName, "_sortinfo.bin"));
                await using FileStream sortInfoFileStream = sortInfoFile.OpenWrite();
                
                bufferStream.Seek(0, SeekOrigin.Begin);
                await bufferStream.CopyToAsync(sortInfoFileStream, cancellationToken);
            }
            
            // Read and output records
            List<(PalmDbRecordHeader Header, FileInfo Path)> records = new();
            for (ushort index = 0;; index++)
            {
                bufferStream.Seek(0, SeekOrigin.Begin);
                bufferStream.SetLength(0);
                
                PalmDbRecordHeader? recordHeader = await recordDb.ReadRecordByIndexAsync(index, bufferStream, cancellationToken);
                if (recordHeader is null)
                {
                    break;
                }

                FileInfo recordFile = new(Path.Combine(outputDirectory.FullName, $"{recordHeader.Id}.bin"));
                await using FileStream recordFileStream = recordFile.OpenWrite();

                bufferStream.Seek(0, SeekOrigin.Begin);
                await bufferStream.CopyToAsync(recordFileStream, cancellationToken);
                
                records.Add((recordHeader, recordFile));
            }
            
            // Read header
            PalmDbHeader header = await recordDb.ReadHeaderAsync(cancellationToken);
            
            // Write manifest
            RecordDbManifest manifest = new(
                header,
                appInfoFile?.Name ?? string.Empty,
                sortInfoFile?.Name ?? string.Empty,
                records.Select(h => new RecordManifest(h.Header, h.Path.Name))
            );
            
            // Write manifest as json
            FileInfo manifestFile = new(Path.Combine(outputDirectory.FullName, "__manifest.json"));
            await using FileStream manifestFileStream = manifestFile.OpenWrite();
            await JsonSerializer.SerializeAsync(manifestFileStream, manifest, cancellationToken: cancellationToken);
        }
    }
}