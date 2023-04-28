using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;
using static Backhand.Dlp.Commands.v1_0.DlpCommands;

namespace Backhand.Dlp.Commands.v1_0
{
    public class DlpClient
    {
        public DlpConnection Connection { get; }

        public DlpClient(DlpConnection connection)
        {
            Connection = connection;
        }

        public override string ToString()
        {
            return Connection.ToString() ?? "DlpClient";
        }

        public async Task<ReadUserInfoResponse> ReadUserInfoAsync(CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadUserInfo,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            return responseArguments.GetValue(ReadUserInfoArguments.Response) ?? throw new Exception("Failed to get ReadUserInfoResult");
        }
        
        public async Task WriteUserInfoAsync(WriteUserInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteUserInfoArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                WriteUserInfo,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<ReadSysInfoResponse> ReadSysInfoAsync(ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadSysInfoArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadSysInfo,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadSysInfoArguments.Response) ?? throw new Exception("Failed to get ReadSysInfoResponse");
        }

        public async Task<ReadSysDateTimeResponse> ReadSysDateTimeAsync(CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadSysDateTime,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadSysDateTimeArguments.Response) ?? throw new Exception("Failed to get ReadSysDateTimeResponse");
        }
        
        public async Task WriteSysDateTimeAsync(WriteSysDateTimeRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteSysDateTimeArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                WriteSysDateTime,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task<(ReadStorageInfoMainResponse, ReadStorageInfoExtResponse)> ReadStorageInfoAsync(ReadStorageInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadStorageInfoArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadStorageInfo,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return (
                responseArguments.GetValue(ReadStorageInfoArguments.MainResponse) ?? throw new Exception("Failed to get ReadStorageInfoMainResponse"),
                responseArguments.GetValue(ReadStorageInfoArguments.ExtResponse) ?? throw new Exception("Failed to get ReadStorageInfoExtResponse")
            );
        }

        public async Task<ReadDbListResponse> ReadDbListAsync(ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadDbListArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadDbList,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadDbListArguments.Response) ?? throw new Exception("Failed to get ReadDbListResponse");
        }

        public async Task<OpenDbResponse> OpenDbAsync(OpenDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(OpenDbArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                OpenDb,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(OpenDbArguments.Response) ?? throw new Exception("Failed to get OpenDbResponse");
        }

        public async Task<CreateDbResponse> CreateDbAsync(CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CreateDbArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                CreateDb,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(CreateDbArguments.Response) ?? throw new Exception("Failed to get CreateDbResponse");
        }

        public async Task CloseDbAsync(CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CloseDbArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                CloseDb,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task DeleteDbAsync(DeleteDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteDbArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                DeleteDb,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<ReadAppBlockResponse> ReadAppBlockAsync(ReadAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadAppBlockArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadAppBlock,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadAppBlockArguments.Response) ?? throw new Exception("Failed to get ReadAppBlockResponse");
        }

        public async Task WriteAppBlockAsync(WriteAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteAppBlockArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                WriteAppBlock,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task<ReadSortBlockResponse> ReadSortBlockAsync(ReadSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadSortBlockArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadSortBlock,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadSortBlockArguments.Response) ?? throw new Exception("Failed to get ReadSortBlockResponse");
        }
        
        public async Task WriteSortBlockAsync(WriteSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteSortBlockArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                WriteSortBlock,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task<ReadNextModifiedRecordResponse> ReadNextModifiedRecordAsync(ReadNextModifiedRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadNextModifiedRecordArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadNextModifiedRecord,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadNextModifiedRecordArguments.Response) ?? throw new Exception("Failed to get ReadNextModifiedRecordResponse");
        }

        public async Task<ReadRecordResponse> ReadRecordByIdAsync(ReadRecordByIdRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordArguments.RequestById, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadRecord,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadRecordArguments.Response) ?? throw new Exception("Failed to get ReadRecordResponse");
        }
        
        public async Task<ReadRecordResponse> ReadRecordByIndexAsync(ReadRecordByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordArguments.RequestByIndex, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadRecord,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadRecordArguments.Response) ?? throw new Exception("Failed to get ReadRecordResponse");
        }
        
        public async Task<WriteRecordResponse> WriteRecordAsync(WriteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteRecordArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                WriteRecord,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(WriteRecordArguments.Response) ?? throw new Exception("Failed to get WriteRecordResponse");
        }
        
        public async Task DeleteRecordAsync(DeleteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteRecordArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                DeleteRecord,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<ReadResourceResponse> ReadResourceByIndexAsync(ReadResourceByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadResourceArguments.RequestByIndex, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadResource,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadResourceArguments.Response) ?? throw new Exception("Failed to get ReadResourceResponse");
        }
        
        public async Task WriteResourceAsync(WriteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteResourceArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                WriteResource,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task DeleteResourceAsync(DeleteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteResourceArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                DeleteResource,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task CleanUpDatabaseAsync(CleanUpDatabaseRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CleanUpDatabaseArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                CleanUpDatabase,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task ResetSyncFlagsAsync(ResetSyncFlagsRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ResetSyncFlagsArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                ResetSyncFlags,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<CallApplicationResponse> CallApplicationAsync(CallApplicationRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CallApplicationArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                CallApplication,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(CallApplicationArguments.Response) ?? throw new Exception("Failed to get CallApplicationResponse");
        }

        public async Task ResetSystemAsync(CancellationToken cancellationToken = default)
        {
            await Connection.ExecuteTransactionAsync(
                ResetSystem,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task AddSyncLogEntryAsync(AddSyncLogEntryRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(AddSyncLogEntryArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                AddSyncLogEntry,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task<ReadOpenDbInfoResponse> ReadOpenDbInfoAsync(ReadOpenDbInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadOpenDbInfoArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadOpenDbInfo,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadOpenDbInfoArguments.Response) ?? throw new Exception("Failed to get ReadOpenDbInfoResponse");
        }

        public async Task MoveCategoryAsync(MoveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(MoveCategoryArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                MoveCategory,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task OpenConduitAsync(CancellationToken cancellationToken = default)
        {
            await Connection.ExecuteTransactionAsync(
                OpenConduit,
                cancellationToken: cancellationToken
            );
        }

        public async Task EndSyncAsync(EndSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(EndSyncArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                EndSync,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task ResetRecordIndexAsync(ResetRecordIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ResetRecordIndexArguments.Request, request);
            
            await Connection.ExecuteTransactionAsync(
                ResetRecordIndex,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
        }
        
        public async Task<ReadRecordIdListResponse> ReadRecordIdListAsync(ReadRecordIdListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordIdListArguments.Request, request);
            
            DlpArgumentMap responseArguments = await Connection.ExecuteTransactionAsync(
                ReadRecordIdList,
                requestArguments,
                cancellationToken
            ).ConfigureAwait(false);
            
            return responseArguments.GetValue(ReadRecordIdListArguments.Response) ?? throw new Exception("Failed to get ReadRecordIdListResponse");
        }
    }
}