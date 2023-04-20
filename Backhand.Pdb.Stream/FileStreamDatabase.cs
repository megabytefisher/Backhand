using Backhand.Common.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backhand.Pdb.Stream
{
    using Stream = System.IO.Stream;

    public class FileStreamDatabase : RecordDatabase<FileStreamRecord>
    {
        private const int BlockContentLength = 4096;
        private const uint BaseRecordId = 0xadb001;

        public FileStreamDatabase()
        {
            Name = "Untitled.TXT";
            Type = ".txt";
            Creator = "FLDB";
            Attributes = DatabaseAttributes.Stream;
        }

        public FileStreamDatabase(string fileName, string type)
        {
            Name = fileName;
            Type = type;
            Creator = "FLDB";
            Attributes = DatabaseAttributes.Stream;
        }

        public async Task WriteInnerFileToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            foreach (FileStreamRecord record in Records)
            {
                await outputStream.WriteAsync(record.Content, cancellationToken);
            }
        }

        public async Task ReadInnerFileFromAsync(Stream inputStream, CancellationToken cancellationToken = default)
        {
            byte[] blockBuffer = new byte[BlockContentLength];

            Records.Clear();
            while (inputStream.Position < inputStream.Length)
            {
                int remainingLength = Convert.ToInt32(inputStream.Length - inputStream.Position);
                int readLength = Math.Min(remainingLength, BlockContentLength);
                await inputStream.FillBufferAsync(blockBuffer.AsMemory().Slice(0, readLength), cancellationToken);

                FileStreamRecord record = new();
                record.Content = new byte[readLength];
                blockBuffer.AsMemory().Slice(0, readLength).CopyTo(record.Content);
                Records.Add(record);
            }
        }
    }
}
