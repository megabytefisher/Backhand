using Backhand.Protocols.Dlp;
using Backhand.Dlp.Commands.v1_0;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Pdb;
using Backhand.Dlp.Commands;
using Backhand.Dlp;

FileStream prcStream = File.OpenRead("Sea_War.prc");

ResourceDatabase database = new();
await database.DeserializeAsync(prcStream);

async Task DoSync(DlpConnection connection, CancellationToken cancellationToken = default)
{
    ReadUserInfoResponse userInfo = await connection.ReadUserInfoAsync(cancellationToken);

    await connection.OpenConduitAsync(cancellationToken);

    List<ReadDbListResponse.DatabaseMetadata> metadataList = new();
    ushort startIndex = 0;
    
    while (true)
    {
        try
        {
            ReadDbListResponse dbListResponse = await connection.ReadDbListAsync(new ReadDbListRequest()
            {
                Mode = ReadDbListRequest.ReadDbListMode.ListRam | ReadDbListRequest.ReadDbListMode.ListMultiple,
                CardId = 0,
                StartIndex = startIndex
            }, cancellationToken);

            metadataList.AddRange(dbListResponse.Results);
            startIndex = (ushort)(dbListResponse.LastIndex + 1);
        }
        catch (DlpCommandErrorException ex)
        {
            if (ex.ErrorCode == DlpErrorCode.NotFoundError)
            {
                break;
            }

            throw;
        }
    }

    Console.WriteLine("OK");

    /*CreateDbResponse createDbResponse = await connection.CreateDbAsync(new()
    {
        Creator = database.Creator,
        Type = database.Type,
        CardId = 0,
        Attributes = (DlpDatabaseAttributes)database.Attributes,
        Version = database.Version,
        Name = database.Name
    }, cancellationToken);

    byte dbHandle = createDbResponse.DbHandle;

    await connection.CloseDbAsync(new CloseDbRequest()
    {
        DbHandle = dbHandle
    }, cancellationToken);*/
}

SerialDlpServer dlpServer = new SerialDlpServer(DoSync, "COM4");
await dlpServer.RunAsync();