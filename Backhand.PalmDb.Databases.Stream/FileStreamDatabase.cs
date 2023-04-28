namespace Backhand.PalmDb.Databases.Stream
{
    using Stream = System.IO.Stream;

    public class FileStreamDatabase
    {
        private readonly IPalmRecordDb _database;
        private const int BlockContentLength = 4096;
        private const uint BaseRecordId = 0xadb001;

        public FileStreamDatabase(IPalmRecordDb database)
        {
            _database = database;
        }
        
        /*public FileStreamDatabase()
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
        }*/

        public async Task WriteInnerFileToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            for (ushort i = 0;; i++)
            {
                (PalmDbRecordHeader Header, FileStreamRecord Data)? record =
                    await _database.ReadRecordByIndexAsync<FileStreamRecord>(i, cancellationToken);
                
                if (record == null) break;
                await outputStream.WriteAsync(record.Value.Data.Content, cancellationToken);
            }
        }

        /*public async Task ReadInnerFileFromAsync(Stream inputStream, CancellationToken cancellationToken = default)
        {
            byte[] blockBuffer = new byte[BlockContentLength];

            while (inputStream.Position < inputStream.Length)
            {
                int remainingLength = Convert.ToInt32(inputStream.Length - inputStream.Position);
                int readLength = Math.Min(remainingLength, BlockContentLength);
                await inputStream.FillBufferAsync(blockBuffer.AsMemory().Slice(0, readLength), cancellationToken);

                FileStreamRecord record = new();
                record.Content = new byte[readLength];
                blockBuffer.AsMemory().Slice(0, readLength).CopyTo(record.Content);
                await _database.WriteRecordAsync(record, cancellationToken);
            }
        }*/
    }
}
