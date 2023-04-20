using Backhand.Pdb.Stream;
using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Cli.Commands.DbCommands
{
    public class ConvertCommand : Command
    {
        public enum ConvertMode
        {
            FileToStreamDb,
            StreamDbToFile
        }

        public readonly Option<FileInfo> InputOption =
            new(new[] { "--input", "-i" }, "The input file to convert")
            {
                IsRequired = true
            };

        public readonly Option<FileInfo?> OutputOption =
            new(new[] { "--output", "-o" }, "The output file to convert");

        public readonly Option<ConvertMode?> ConvertModeOption =
            new(new[] { "--mode", "-m" }, "The conversion mode");

        public ConvertCommand()
            : base("convert", "Converts Palm database files to/from other file formats")
        {
            Add(InputOption);
            Add(OutputOption);
            Add(ConvertModeOption);

            this.SetHandler(async (context) =>
            {
                FileInfo input = context.ParseResult.GetValueForOption(InputOption)!;
                FileInfo? output = context.ParseResult.GetValueForOption(OutputOption);
                ConvertMode? mode = context.ParseResult.GetValueForOption(ConvertModeOption)!;

                IConsole console = context.Console;
                CancellationToken cancellationToken = context.GetCancellationToken();

                if (mode == null)
                {
                    if (output == null)
                    {
                        console.WriteLine("Output file must be specified when mode is not specified.");
                        return;
                    }

                    mode = (input.Extension.ToLower(), output.Extension.ToLower()) switch
                    {
                        (".pdb", ".jpg") => ConvertMode.StreamDbToFile,
                        (".jpg", ".pdb") => ConvertMode.FileToStreamDb,
                        _ => throw new InvalidOperationException("Unknown conversion mode")
                    };
                }

                switch (mode)
                {
                    case ConvertMode.FileToStreamDb:
                        await ConvertFileToStreamDbAsync(input, output, cancellationToken).ConfigureAwait(false);
                        break;
                    case ConvertMode.StreamDbToFile:
                        await ConvertStreamDbToFileAsync(input, output, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown conversion mode: {mode}");
                }
            });
        }

        private static async Task ConvertStreamDbToFileAsync(FileInfo inputFile, FileInfo? outputFile, CancellationToken cancellationToken)
        {
            using FileStream inputStream = inputFile.OpenRead();

            FileStreamDatabase database = new();
            await database.DeserializeAsync(inputStream, cancellationToken).ConfigureAwait(false);

            outputFile = outputFile ?? new FileInfo(database.Name);
            using FileStream outputStream = outputFile.OpenWrite();

            await database.WriteInnerFileToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ConvertFileToStreamDbAsync(FileInfo inputFile, FileInfo? outputFile, CancellationToken cancellationToken)
        {
            using FileStream inputStream = inputFile.OpenRead();

            outputFile = outputFile ?? new FileInfo(Path.ChangeExtension(inputFile.Name, ".pdb"));
            using FileStream outputStream = outputFile.OpenWrite();

            FileStreamDatabase database = new(inputFile.Name, inputFile.Extension.ToLower());
            await database.ReadInnerFileFromAsync(inputStream, cancellationToken).ConfigureAwait(false);
            await database.SerializeAsync(outputStream, cancellationToken).ConfigureAwait(false);
        }
    }
}
