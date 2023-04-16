using Backhand.Protocols.Dlp;
using Backhand.Dlp.Commands.v1_0.Arguments;
using System.Threading.Tasks;
using System.Threading;
using System;

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

        public static readonly DlpCommandDefinition ReadUserInfo = new DlpCommandDefinition(
            DlpOpcodes.ReadUserInfo,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { ReadUserInfoArguments.Response }
        );

        public static async Task<ReadUserInfoResponse> ReadUserInfoAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadUserInfo, cancellationToken: cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadUserInfoArguments.Response) ?? throw new Exception("Failed to get ReadUserInfoResult");
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

        public static readonly DlpCommandDefinition ReadSysInfo = new DlpCommandDefinition(
            DlpOpcodes.ReadSysInfo,
            new DlpArgumentDefinition[] { ReadSysInfoArguments.Request },
            new DlpArgumentDefinition[] { ReadSysInfoArguments.SystemResponse, ReadSysInfoArguments.DlpResponse }
        );

        public static async Task<(ReadSysInfoSystemResponse, ReadSysInfoDlpResponse)> ReadSysInfoAsync(this DlpConnection connection, ReadSysInfoRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadSysInfoArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadSysInfo, requestArguments, cancellationToken);

            return (
                responseArguments.GetValue(DlpCommands.ReadSysInfoArguments.SystemResponse) ?? throw new Exception("Failed to get ReadSysInfoSystemResult"),
                responseArguments.GetValue(DlpCommands.ReadSysInfoArguments.DlpResponse) ?? throw new Exception("Failed to get ReadSysInfoDlpResult")
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

        public static readonly DlpCommandDefinition ReadDbList = new DlpCommandDefinition(
            DlpOpcodes.ReadDbList,
            new DlpArgumentDefinition[] { ReadDbListArguments.Request },
            new DlpArgumentDefinition[] { ReadDbListArguments.Response }
        );

        public static async Task<ReadDbListResponse> ReadDbListAsync(this DlpConnection connection, ReadDbListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadDbListArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadDbList, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadDbListArguments.Response) ?? throw new Exception("Failed to get ReadDbListResult");
        }

        /************************************/
        /*  OpenDb                          */
        /************************************/
        public static class OpenDbArguments
        {
            public static readonly DlpArgumentDefinition<OpenDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<OpenDbResponse> Response = new();
        }

        public static readonly DlpCommandDefinition OpenDb = new DlpCommandDefinition(
            DlpOpcodes.OpenDb,
            new DlpArgumentDefinition[] { OpenDbArguments.Request },
            new DlpArgumentDefinition[] { OpenDbArguments.Response }
        );

        public static async Task<OpenDbResponse> OpenDbAsync(this DlpConnection connection, OpenDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.OpenDbArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.OpenDb, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.OpenDbArguments.Response) ?? throw new Exception("Failed to get OpenDbResult");
        }

        /************************************/
        /*  CreateDb                        */
        /************************************/
        public static class CreateDbArguments
        {
            public static readonly DlpArgumentDefinition<CreateDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<CreateDbResponse> Response = new();
        }

        public static readonly DlpCommandDefinition CreateDb = new DlpCommandDefinition(
            DlpOpcodes.CreateDb,
            new DlpArgumentDefinition[] { CreateDbArguments.Request },
            new DlpArgumentDefinition[] { CreateDbArguments.Response }
        );

        public static async Task<CreateDbResponse> CreateDbAsync(this DlpConnection connection, CreateDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.CreateDbArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.CreateDb, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.CreateDbArguments.Response) ?? throw new Exception("Failed to get CreateDbResult");
        }

        /************************************/
        /*  CloseDb                         */
        /************************************/
        public static class CloseDbArguments
        {
            public static readonly DlpArgumentDefinition<CloseDbRequest> Request = new();
        }

        public static readonly DlpCommandDefinition CloseDb = new DlpCommandDefinition(
            DlpOpcodes.CloseDb,
            new DlpArgumentDefinition[] { CloseDbArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task CloseDbAsync(this DlpConnection connection, CloseDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.CloseDbArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.CloseDb, requestArguments, cancellationToken);
        }

        /************************************/
        /*  DeleteDb                        */
        /************************************/
        public static class DeleteDbArguments
        {
            public static readonly DlpArgumentDefinition<DeleteDbRequest> Request = new();
        }

        public static readonly DlpCommandDefinition DeleteDb = new DlpCommandDefinition(
            DlpOpcodes.DeleteDb,
            new DlpArgumentDefinition[] { DeleteDbArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task DeleteDbAsync(this DlpConnection connection, DeleteDbRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.DeleteDbArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.DeleteDb, requestArguments, cancellationToken);
        }

        /************************************/
        /*  ReadAppBlock                    */
        /************************************/
        public static class ReadAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadAppBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadAppBlockResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadAppBlock = new DlpCommandDefinition(
            DlpOpcodes.ReadAppBlock,
            new DlpArgumentDefinition[] { ReadAppBlockArguments.Request },
            new DlpArgumentDefinition[] { ReadAppBlockArguments.Response }
        );

        public static async Task<ReadAppBlockResponse> ReadAppBlockAsync(this DlpConnection connection, ReadAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadAppBlockArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadAppBlock, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadAppBlockArguments.Response) ?? throw new Exception("Failed to get ReadAppBlockResult");
        }

        /************************************/
        /*  WriteAppBlock                   */
        /************************************/
        public static class WriteAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteAppBlockRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteAppBlock = new DlpCommandDefinition(
            DlpOpcodes.WriteAppBlock,
            new DlpArgumentDefinition[] { WriteAppBlockArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteAppBlockAsync(this DlpConnection connection, WriteAppBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.WriteAppBlockArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.WriteAppBlock, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadSortBlock                    */
        /************************************/
        public static class ReadSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadSortBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSortBlockResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadSortBlock = new DlpCommandDefinition(
            DlpOpcodes.ReadSortBlock,
            new DlpArgumentDefinition[] { ReadSortBlockArguments.Request },
            new DlpArgumentDefinition[] { ReadSortBlockArguments.Response }
        );

        public static async Task<ReadSortBlockResponse> ReadSortBlockAsync(this DlpConnection connection, ReadSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadSortBlockArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadSortBlock, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadSortBlockArguments.Response) ?? throw new Exception("Failed to get ReadSortBlockResult");
        }

        /************************************/
        /* WriteSortBlock                   */
        /************************************/
        public static class WriteSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteSortBlockRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteSortBlock = new DlpCommandDefinition(
            DlpOpcodes.WriteSortBlock,
            new DlpArgumentDefinition[] { WriteSortBlockArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteSortBlockAsync(this DlpConnection connection, WriteSortBlockRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.WriteSortBlockArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.WriteSortBlock, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadRecordById                   */
        /************************************/
        public static class ReadRecordByIdArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordByIdRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadRecordByIdResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadRecordById = new DlpCommandDefinition(
            DlpOpcodes.ReadRecord,
            new DlpArgumentDefinition[] { ReadRecordByIdArguments.Request },
            new DlpArgumentDefinition[] { ReadRecordByIdArguments.Response }
        );

        public static async Task<ReadRecordByIdResponse> ReadRecordByIdAsync(this DlpConnection connection, ReadRecordByIdRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadRecordByIdArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadRecordById, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadRecordByIdArguments.Response) ?? throw new Exception("Failed to get ReadRecordByIdResult");
        }

        /************************************/
        /* WriteRecord                      */
        /************************************/
        public static class WriteRecordArguments
        {
            public static readonly DlpArgumentDefinition<WriteRecordRequest> Request = new();
            public static readonly DlpArgumentDefinition<WriteRecordResponse> Response = new();
        }

        public static readonly DlpCommandDefinition WriteRecord = new DlpCommandDefinition(
            DlpOpcodes.WriteRecord,
            new DlpArgumentDefinition[] { WriteRecordArguments.Request },
            new DlpArgumentDefinition[] { WriteRecordArguments.Response }
        );

        public static async Task<WriteRecordResponse> WriteRecordAsync(this DlpConnection connection, WriteRecordRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.WriteRecordArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.WriteRecord, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.WriteRecordArguments.Response) ?? throw new Exception("Failed to get WriteRecordResult");
        }
        
        /************************************/
        /* ReadResource                     */
        /************************************/
        public static class ReadResourceArguments
        {
            public static readonly DlpArgumentDefinition<ReadResourceByIndexRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadResourceByIndexResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadResource = new DlpCommandDefinition(
            DlpOpcodes.ReadResource,
            new DlpArgumentDefinition[] { ReadResourceArguments.Request },
            new DlpArgumentDefinition[] { ReadResourceArguments.Response }
        );

        public static async Task<ReadResourceByIndexResponse> ReadResourceAsync(this DlpConnection connection, ReadResourceByIndexRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadResourceArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadResource, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadResourceArguments.Response) ?? throw new Exception("Failed to get ReadResourceResult");
        }

        /************************************/
        /* WriteResource                    */
        /************************************/
        public static class WriteResourceArguments
        {
            public static readonly DlpArgumentDefinition<WriteResourceRequest> Request = new();
        }

        public static readonly DlpCommandDefinition WriteResource = new DlpCommandDefinition(
            DlpOpcodes.WriteResource,
            new DlpArgumentDefinition[] { WriteResourceArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task WriteResourceAsync(this DlpConnection connection, WriteResourceRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.WriteResourceArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.WriteResource, requestArguments, cancellationToken);
        }

        /************************************/
        /* OpenConduit                      */
        /************************************/
        public static readonly DlpCommandDefinition OpenConduit = new DlpCommandDefinition(
            DlpOpcodes.OpenConduit,
            new DlpArgumentDefinition[] { },
            new DlpArgumentDefinition[] { }
        );

        public static async Task OpenConduitAsync(this DlpConnection connection, CancellationToken cancellationToken = default)
        {
            await connection.ExecuteTransactionAsync(DlpCommands.OpenConduit, cancellationToken: cancellationToken);
        }

        /************************************/
        /* EndSync                          */
        /************************************/
        public static class EndSyncArguments
        {
            public static readonly DlpArgumentDefinition<EndSyncRequest> Request = new();
        }

        public static readonly DlpCommandDefinition EndSync = new DlpCommandDefinition(
            DlpOpcodes.EndSync,
            new DlpArgumentDefinition[] { EndSyncArguments.Request },
            new DlpArgumentDefinition[] { }
        );

        public static async Task EndSyncAsync(this DlpConnection connection, EndSyncRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.EndSyncArguments.Request, request);

            await connection.ExecuteTransactionAsync(DlpCommands.EndSync, requestArguments, cancellationToken);
        }

        /************************************/
        /* ReadRecordIdList                 */
        /************************************/
        public static class ReadRecordIdListArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> Response = new();
        }

        public static readonly DlpCommandDefinition ReadRecordIdList = new DlpCommandDefinition(
            DlpOpcodes.ReadRecordIdList,
            new DlpArgumentDefinition[] { ReadRecordIdListArguments.Request },
            new DlpArgumentDefinition[] { ReadRecordIdListArguments.Response }
        );

        public static async Task<ReadRecordIdListResponse> ReadRecordIdListAsync(this DlpConnection connection, ReadRecordIdListRequest request, CancellationToken cancellationToken = default)
        {
            DlpArgumentMap requestArguments = new DlpArgumentMap();
            requestArguments.SetValue(DlpCommands.ReadRecordIdListArguments.Request, request);

            DlpArgumentMap responseArguments = await connection.ExecuteTransactionAsync(DlpCommands.ReadRecordIdList, requestArguments, cancellationToken);

            return responseArguments.GetValue(DlpCommands.ReadRecordIdListArguments.Response) ?? throw new Exception("Failed to get ReadRecordIdListResult");
        }
    }
}
