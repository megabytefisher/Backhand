using System;
using System.Threading;
using System.Threading.Tasks;
using Backhand.Dlp.Commands.v1_0.Arguments;
using Backhand.Protocols.Dlp;

namespace Backhand.Dlp.Commands.v1_0
{
    public static class DlpCommands
    {
        /************************************/
        /*  ReadUserInfo                    */
        /************************************/
        public static class ReadUserInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadUserInfoResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadUserInfo = new(
            DlpOpcodes.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadUserInfoArguments.Response }
        );

        public static async Task<ReadUserInfoResponse> ReadUserInfoAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadUserInfo, cancellationToken: cancellationToken);

            return responseArguments.GetValue(ReadUserInfoArguments.Response) ?? throw new Exception("Failed to get ReadUserInfoResult");
        }

        /************************************/
        /*  WriteUserInfo                   */
        /************************************/
        public static class WriteUserInfoArguments
        {
            public static readonly DlpArgumentDefinition<WriteUserInfoRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteUserInfo = new(
            DlpOpcodes.WriteUserInfo,
            new DlpArgumentDefinition[] { WriteUserInfoArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteUserInfoAsync(this DlpConnection connection, WriteUserInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteUserInfoArguments.Request, request);

            await connection.ExecuteTransactionAsync(WriteUserInfo, requestArguments, cancellationToken);
        }

        /************************************/
        /*  ReadSysInfo                     */
        /************************************/
        public static class ReadSysInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoSystemResponse> SystemResponse = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoDlpResponse> DlpResponse = new();
        }

        public static readonly DlpCommandDefinition ReadSysInfo = new(
            DlpOpcodes.ReadSysInfo,
            new DlpArgumentDefinition[] { ReadSysInfoArguments.Request },
            new DlpArgumentDefinition[] { ReadSysInfoArguments.SystemResponse, ReadSysInfoArguments.DlpResponse }
        );

        public static async Task<(ReadSysInfoSystemResponse, ReadSysInfoDlpResponse)> ReadSysInfoAsync(this DlpConnection connection, ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadSysInfoArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadSysInfo, requestArguments, cancellationToken);

            return (
                responseArguments.GetValue(ReadSysInfoArguments.SystemResponse) ?? throw new Exception("Failed to get ReadSysInfoSystemResult"),
                responseArguments.GetValue(ReadSysInfoArguments.DlpResponse) ?? throw new Exception("Failed to get ReadSysInfoDlpResult")
            );
        }

        /************************************/
        /*  ReadSysDateTime                 */
        /************************************/
        public static class ReadSysDateTimeArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysDateTimeResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadSysDateTime = new(
            DlpOpcodes.ReadSysDateTime,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadSysDateTimeArguments.Response }
        );

        public static async Task<ReadSysDateTimeResponse> ReadSysDateTimeAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadSysDateTime, cancellationToken: cancellationToken);

            return responseArguments.GetValue(ReadSysDateTimeArguments.Response) ?? throw new Exception("Failed to get ReadSysDateTimeResult");
        }

        /************************************/
        /*  WriteSysDateTime                */
        /************************************/
        public static class WriteSysDateTimeArguments
        {
            public static readonly DlpArgumentDefinition<WriteSysDateTimeRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteSysDateTime = new(
            DlpOpcodes.WriteSysDateTime,
            new DlpArgumentDefinition[] { WriteSysDateTimeArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteSysDateTimeAsync(this DlpConnection connection, WriteSysDateTimeRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteSysDateTimeArguments.Request, request);

            await connection.ExecuteTransactionAsync(WriteSysDateTime, requestArguments, cancellationToken);
        }

        /************************************/
        /*  ReadStorageInfo                 */
        /************************************/
        public static class ReadStorageInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadStorageInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadStorageInfoMainResponse> MainResponse = new();
            public static readonly DlpArgumentDefinition<ReadStorageInfoExtResponse> ExtResponse = new();
        }

        public static readonly DlpCommandDefinition ReadStorageInfo = new(
            DlpOpcodes.ReadStorageInfo,
            new DlpArgumentDefinition[] { ReadStorageInfoArguments.Request },
            new DlpArgumentDefinition[] { ReadStorageInfoArguments.MainResponse, ReadStorageInfoArguments.ExtResponse }
        );

        public static async Task<(ReadStorageInfoMainResponse, ReadStorageInfoExtResponse)> ReadStorageInfoAsync(this DlpConnection connection, ReadStorageInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadStorageInfoArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadStorageInfo, requestArguments, cancellationToken);

            return (
                responseArguments.GetValue(ReadStorageInfoArguments.MainResponse) ?? throw new Exception("Failed to get ReadStorageInfoMainResult"),
                responseArguments.GetValue(ReadStorageInfoArguments.ExtResponse) ?? throw new Exception("Failed to get ReadStorageInfoExtResult")
            );
        }

        /************************************/
        /*  ReadDbList                      */
        /************************************/
        public static class ReadDbListArguments
        {
            public static readonly DlpArgumentDefinition<ReadDbListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadDbListResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadDbList = new(
            DlpOpcodes.ReadDbList,
            new DlpArgumentDefinition[] { ReadDbListArguments.Request },
            new DlpArgumentDefinition[] { ReadDbListArguments.Response }
        );

        public static async Task<ReadDbListResponse> ReadDbListAsync(this DlpConnection connection, ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadDbListArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadDbList, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadDbListArguments.Response) ?? throw new Exception("Failed to get ReadDbListResult");
        }

        /************************************/
        /*  OpenDb                          */
        /************************************/
        public static class OpenDbArguments
        {
            public static readonly DlpArgumentDefinition<OpenDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<OpenDbResponse> Response = new();
        }

        public static readonly DlpCommandDefinition OpenDb = new(
            DlpOpcodes.OpenDb,
            new DlpArgumentDefinition[] { OpenDbArguments.Request },
            new DlpArgumentDefinition[] { OpenDbArguments.Response }
        );

        public static async Task<OpenDbResponse> OpenDbAsync(this DlpConnection connection, OpenDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(OpenDbArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(OpenDb, requestArguments, cancellationToken);

            return responseArguments.GetValue(OpenDbArguments.Response) ?? throw new Exception("Failed to get OpenDbResult");
        }

        /************************************/
        /*  CreateDb                        */
        /************************************/
        public static class CreateDbArguments
        {
            public static readonly DlpArgumentDefinition<CreateDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<CreateDbResponse> Response = new();
        }

        public static readonly DlpCommandDefinition CreateDb = new(
            DlpOpcodes.CreateDb,
            new DlpArgumentDefinition[] { CreateDbArguments.Request },
            new DlpArgumentDefinition[] { CreateDbArguments.Response }
        );

        public static async Task<CreateDbResponse> CreateDbAsync(this DlpConnection connection, CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CreateDbArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(CreateDb, requestArguments, cancellationToken);

            return responseArguments.GetValue(CreateDbArguments.Response) ?? throw new Exception("Failed to get CreateDbResult");
        }

        /************************************/
        /*  CloseDb                         */
        /************************************/
        public static class CloseDbArguments
        {
            public static readonly DlpArgumentDefinition<CloseDbRequest> Request = new();
        }

        public static readonly DlpCommandDefinition CloseDb = new(
            DlpOpcodes.CloseDb,
            new DlpArgumentDefinition[] { CloseDbArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task CloseDbAsync(this DlpConnection connection, CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CloseDbArguments.Request, request);

            await connection.ExecuteTransactionAsync(CloseDb, requestArguments, cancellationToken);
        }

        /************************************/
        /*  DeleteDb                        */
        /************************************/
        public static class DeleteDbArguments
        {
            public static readonly DlpArgumentDefinition<DeleteDbRequest> Request = new();
        }

        public static readonly DlpCommandDefinition DeleteDb = new(
            DlpOpcodes.DeleteDb,
            new DlpArgumentDefinition[] { DeleteDbArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task DeleteDbAsync(this DlpConnection connection, DeleteDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteDbArguments.Request, request);

            await connection.ExecuteTransactionAsync(DeleteDb, requestArguments, cancellationToken);
        }

        /************************************/
        /*  ReadAppBlock                    */
        /************************************/
        public static class ReadAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadAppBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadAppBlockResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadAppBlock = new(
            DlpOpcodes.ReadAppBlock,
            new DlpArgumentDefinition[] { ReadAppBlockArguments.Request },
            new DlpArgumentDefinition[] { ReadAppBlockArguments.Response }
        );

        public static async Task<ReadAppBlockResponse> ReadAppBlockAsync(this DlpConnection connection, ReadAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadAppBlockArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadAppBlock, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadAppBlockArguments.Response) ?? throw new Exception("Failed to get ReadAppBlockResult");
        }

        /************************************/
        /*  WriteAppBlock                   */
        /************************************/
        public static class WriteAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteAppBlockRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteAppBlock = new(
            DlpOpcodes.WriteAppBlock,
            new DlpArgumentDefinition[] { WriteAppBlockArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteAppBlockAsync(this DlpConnection connection, WriteAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteAppBlockArguments.Request, request);

            await connection.ExecuteTransactionAsync(WriteAppBlock, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadSortBlock                    */
        /************************************/
        public static class ReadSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadSortBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSortBlockResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadSortBlock = new(
            DlpOpcodes.ReadSortBlock,
            new DlpArgumentDefinition[] { ReadSortBlockArguments.Request },
            new DlpArgumentDefinition[] { ReadSortBlockArguments.Response }
        );

        public static async Task<ReadSortBlockResponse> ReadSortBlockAsync(this DlpConnection connection, ReadSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadSortBlockArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadSortBlock, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadSortBlockArguments.Response) ?? throw new Exception("Failed to get ReadSortBlockResult");
        }

        /************************************/
        /* WriteSortBlock                   */
        /************************************/
        public static class WriteSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteSortBlockRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteSortBlock = new(
            DlpOpcodes.WriteSortBlock,
            new DlpArgumentDefinition[] { WriteSortBlockArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteSortBlockAsync(this DlpConnection connection, WriteSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteSortBlockArguments.Request, request);

            await connection.ExecuteTransactionAsync(WriteSortBlock, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadNextModifiedRecord           */
        /************************************/
        public static class ReadNextModifiedRecordArguments
        {
            public static readonly DlpArgumentDefinition<ReadNextModifiedRecordRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadNextModifiedRecordResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadNextModifiedRecord = new(
            DlpOpcodes.ReadNextModifiedRecord,
            new DlpArgumentDefinition[] { ReadNextModifiedRecordArguments.Request },
            new DlpArgumentDefinition[] { ReadNextModifiedRecordArguments.Response }
        );

        public static async Task<ReadNextModifiedRecordResponse> ReadNextModifiedRecordAsync(this DlpConnection connection, ReadNextModifiedRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadNextModifiedRecordArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadNextModifiedRecord, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadNextModifiedRecordArguments.Response) ?? throw new Exception("Failed to get ReadNextModifiedRecordResult");
        }

        /************************************/
        /* ReadRecord                       */
        /************************************/
        public static class ReadRecordArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordByIdRequest> RequestById = new();
            public static readonly DlpArgumentDefinition<ReadRecordByIndexRequest> RequestByIndex = new();
            public static readonly DlpArgumentDefinition<ReadRecordResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadRecord = new(
            DlpOpcodes.ReadRecord,
            new DlpArgumentDefinition[] { ReadRecordArguments.RequestById, ReadRecordArguments.RequestByIndex },
            new DlpArgumentDefinition[] { ReadRecordArguments.Response }
        );

        public static async Task<ReadRecordResponse> ReadRecordByIdAsync(this DlpConnection connection, ReadRecordByIdRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordArguments.RequestById, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadRecord, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadRecordArguments.Response) ?? throw new Exception("Failed to get ReadRecordByIdResult");
        }
        
        public static async Task<ReadRecordResponse> ReadRecordByIndexAsync(this DlpConnection connection, ReadRecordByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordArguments.RequestByIndex, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadRecord, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadRecordArguments.Response) ?? throw new Exception("Failed to get ReadRecordByIndexResult");
        }

        /************************************/
        /* WriteRecord                      */
        /************************************/
        public static class WriteRecordArguments
        {
            public static readonly DlpArgumentDefinition<WriteRecordRequest> Request = new();
            public static readonly DlpArgumentDefinition<WriteRecordResponse> Response = new();
        }

        public static readonly DlpCommandDefinition WriteRecord = new(
            DlpOpcodes.WriteRecord,
            new DlpArgumentDefinition[] { WriteRecordArguments.Request },
            new DlpArgumentDefinition[] { WriteRecordArguments.Response }
        );

        public static async Task<WriteRecordResponse> WriteRecordAsync(this DlpConnection connection, WriteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteRecordArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(WriteRecord, requestArguments, cancellationToken);

            return responseArguments.GetValue(WriteRecordArguments.Response) ?? throw new Exception("Failed to get WriteRecordResult");
        }

        /************************************/
        /* DeleteRecord                     */
        /************************************/
        public static class DeleteRecordArguments
        {
            public static readonly DlpArgumentDefinition<DeleteRecordRequest> Request = new();
        }

        public static readonly DlpCommandDefinition DeleteRecord = new(
            DlpOpcodes.DeleteRecord,
            new DlpArgumentDefinition[] { DeleteRecordArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task DeleteRecordAsync(this DlpConnection connection, DeleteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteRecordArguments.Request, request);

            await connection.ExecuteTransactionAsync(DeleteRecord, requestArguments, cancellationToken);
        }
        
        /************************************/
        /* ReadResource                     */
        /************************************/
        public static class ReadResourceArguments
        {
            public static readonly DlpArgumentDefinition<ReadResourceByIndexRequest> RequestByIndex = new();
            public static readonly DlpArgumentDefinition<ReadResourceResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadResource = new(
            DlpOpcodes.ReadResource,
            new DlpArgumentDefinition[] { ReadResourceArguments.RequestByIndex },
            new DlpArgumentDefinition[] { ReadResourceArguments.Response }
        );

        public static async Task<ReadResourceResponse> ReadResourceByIndexAsync(this DlpConnection connection, ReadResourceByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadResourceArguments.RequestByIndex, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadResource, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadResourceArguments.Response) ?? throw new Exception("Failed to get ReadResourceResult");
        }

        /************************************/
        /* WriteResource                    */
        /************************************/
        public static class WriteResourceArguments
        {
            public static readonly DlpArgumentDefinition<WriteResourceRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteResource = new(
            DlpOpcodes.WriteResource,
            new DlpArgumentDefinition[] { WriteResourceArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteResourceAsync(this DlpConnection connection, WriteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(WriteResourceArguments.Request, request);

            await connection.ExecuteTransactionAsync(WriteResource, requestArguments, cancellationToken);
        }

        /************************************/
        /* DeleteResource                   */
        /**************************************/
        public static class DeleteResourceArguments
        {
            public static readonly DlpArgumentDefinition<DeleteResourceRequest> Request = new();
        }

        public static readonly DlpCommandDefinition DeleteResource = new(
            DlpOpcodes.DeleteResource,
            new DlpArgumentDefinition[] { DeleteResourceArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task DeleteResourceAsync(this DlpConnection connection, DeleteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(DeleteResourceArguments.Request, request);

            await connection.ExecuteTransactionAsync(DeleteResource, requestArguments, cancellationToken);
        }

        /************************************/
        /* CleanUpDatabase                  */
        /************************************/
        public static class CleanUpDatabaseArguments
        {
            public static readonly DlpArgumentDefinition<CleanUpDatabaseRequest> Request = new();
        }

        public static readonly DlpCommandDefinition CleanUpDatabase = new(
            DlpOpcodes.CleanUpDatabase,
            new DlpArgumentDefinition[] { CleanUpDatabaseArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task CleanUpDatabaseAsync(this DlpConnection connection, CleanUpDatabaseRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CleanUpDatabaseArguments.Request, request);

            await connection.ExecuteTransactionAsync(CleanUpDatabase, requestArguments, cancellationToken);
        }

        /************************************/
        /* ResetSyncFlags                   */
        /************************************/
        public static class ResetSyncFlagsArguments
        {
            public static readonly DlpArgumentDefinition<ResetSyncFlagsRequest> Request = new();
        }

        public static readonly DlpCommandDefinition ResetSyncFlags = new(
            DlpOpcodes.ResetSyncFlags,
            new DlpArgumentDefinition[] { ResetSyncFlagsArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task ResetSyncFlagsAsync(this DlpConnection connection, ResetSyncFlagsRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ResetSyncFlagsArguments.Request, request);

            await connection.ExecuteTransactionAsync(ResetSyncFlags, requestArguments, cancellationToken);
        }

        /************************************/
        /* CallApplication                  */
        /************************************/
        public static class CallApplicationArguments
        {
            public static readonly DlpArgumentDefinition<CallApplicationRequest> Request = new();
            public static readonly DlpArgumentDefinition<CallApplicationResponse> Response = new();
        }

        public static readonly DlpCommandDefinition CallApplication = new(
            DlpOpcodes.CallApplication,
            new DlpArgumentDefinition[] { CallApplicationArguments.Request },
            new DlpArgumentDefinition[] { CallApplicationArguments.Response }
        );

        public static async Task<CallApplicationResponse> CallApplicationAsync(this DlpConnection connection, CallApplicationRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(CallApplicationArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(CallApplication, requestArguments, cancellationToken);

            return responseArguments.GetValue(CallApplicationArguments.Response) ?? throw new Exception("Failed to get CallApplicationResult");
        }

        /************************************/
        /* ResetSystem                      */
        /************************************/
        public static readonly DlpCommandDefinition ResetSystem = new(
            DlpOpcodes.ResetSystem,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { }
        );

        public static async Task ResetSystemAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            await connection.ExecuteTransactionAsync(ResetSystem, cancellationToken: cancellationToken);
        }

        /************************************/
        /* AddSyncLogEntry                  */
        /************************************/
        public static class AddSyncLogEntryArguments
        {
            public static readonly DlpArgumentDefinition<AddSyncLogEntryRequest> Request = new();
        }

        public static readonly DlpCommandDefinition AddSyncLogEntry = new(
            DlpOpcodes.AddSyncLogEntry,
            new DlpArgumentDefinition[] { AddSyncLogEntryArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task AddSyncLogEntryAsync(this DlpConnection connection, AddSyncLogEntryRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(AddSyncLogEntryArguments.Request, request);

            await connection.ExecuteTransactionAsync(AddSyncLogEntry, requestArguments, cancellationToken);
        }
        
        /************************************/
        /* ReadOpenDbInfo                   */
        /************************************/
        public static class ReadOpenDbInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadOpenDbInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadOpenDbInfoResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadOpenDbInfo = new(
            DlpOpcodes.ReadOpenDbInfo,
            new DlpArgumentDefinition[] { ReadOpenDbInfoArguments.Request },
            new DlpArgumentDefinition[] { ReadOpenDbInfoArguments.Response }
        );

        public static async Task<ReadOpenDbInfoResponse> ReadOpenDbInfoAsync(this DlpConnection connection, ReadOpenDbInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadOpenDbInfoArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadOpenDbInfo, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadOpenDbInfoArguments.Response) ?? throw new Exception("Failed to get ReadOpenDbInfoResult");
        }

        /************************************/
        /* MoveCategory                     */
        /************************************/
        public static class MoveCategoryArguments
        {
            public static readonly DlpArgumentDefinition<MoveCategoryRequest> Request = new();
        }

        public static readonly DlpCommandDefinition MoveCategory = new(
            DlpOpcodes.MoveCategory,
            new DlpArgumentDefinition[] { MoveCategoryArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task MoveCategoryAsync(this DlpConnection connection, MoveCategoryRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(MoveCategoryArguments.Request, request);

            await connection.ExecuteTransactionAsync(MoveCategory, requestArguments, cancellationToken);
        }

        /************************************/
        /* OpenConduit                      */
        /************************************/
        public static readonly DlpCommandDefinition OpenConduit = new(
            DlpOpcodes.OpenConduit,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { }
        );

        public static async Task OpenConduitAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            await connection.ExecuteTransactionAsync(OpenConduit, cancellationToken: cancellationToken);
        }

        /************************************/
        /* EndSync                          */
        /************************************/
        public static class EndSyncArguments
        {
            public static readonly DlpArgumentDefinition<EndSyncRequest> Request = new();
        }

        public static readonly DlpCommandDefinition EndSync = new(
            DlpOpcodes.EndSync,
            new DlpArgumentDefinition[] { EndSyncArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task EndSyncAsync(this DlpConnection connection, EndSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(EndSyncArguments.Request, request);

            await connection.ExecuteTransactionAsync(EndSync, requestArguments, cancellationToken);
        }

        /************************************/
        /* ResetRecordIndex                 */
        /************************************/
        public static class ResetRecordIndexArguments
        {
            public static readonly DlpArgumentDefinition<ResetRecordIndexRequest> Request = new();
        }

        public static readonly DlpCommandDefinition ResetRecordIndex = new(
            DlpOpcodes.ResetRecordIndex,
            new DlpArgumentDefinition[] { ResetRecordIndexArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task ResetRecordIndexAsync(this DlpConnection connection, ResetRecordIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ResetRecordIndexArguments.Request, request);

            await connection.ExecuteTransactionAsync(ResetRecordIndex, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadRecordIdList                 */
        /************************************/
        public static class ReadRecordIdListArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadRecordIdList = new(
            DlpOpcodes.ReadRecordIdList,
            new DlpArgumentDefinition[] { ReadRecordIdListArguments.Request },
            new DlpArgumentDefinition[] { ReadRecordIdListArguments.Response }
        );

        public static async Task<ReadRecordIdListResponse> ReadRecordIdListAsync(this DlpConnection connection, ReadRecordIdListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new();
            requestArguments.SetValue(ReadRecordIdListArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(ReadRecordIdList, requestArguments, cancellationToken);

            return responseArguments.GetValue(ReadRecordIdListArguments.Response) ?? throw new Exception("Failed to get ReadRecordIdListResult");
        }
    }
}
