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

        public static readonly DlpCommandDefinition ReadUserInfo = new()
        {
            Opcode = DlpOpcodes.ReadUserInfo,
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadUserInfoArguments.Response
            }
        };

        /************************************/
        /*  WriteUserInfo                   */
        /************************************/
        public static class WriteUserInfoArguments
        {
            public static readonly DlpArgumentDefinition<WriteUserInfoRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition WriteUserInfo = new()
        {
            Opcode = DlpOpcodes.WriteUserInfo,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteUserInfoArguments.Request
            }
        };

        /************************************/
        /*  ReadSysInfo                     */
        /************************************/
        public static class ReadSysInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSysInfoResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadSysInfo = new()
        {
            Opcode = DlpOpcodes.ReadSysInfo,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadSysInfoArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadSysInfoArguments.Response,
            }
        };

        /************************************/
        /*  ReadSysDateTime                 */
        /************************************/
        public static class ReadSysDateTimeArguments
        {
            public static readonly DlpArgumentDefinition<ReadSysDateTimeResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadSysDateTime = new()
        {
            Opcode = DlpOpcodes.ReadSysDateTime,
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadSysDateTimeArguments.Response
            }
        };

        /************************************/
        /*  WriteSysDateTime                */
        /************************************/
        public static class WriteSysDateTimeArguments
        {
            public static readonly DlpArgumentDefinition<WriteSysDateTimeRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition WriteSysDateTime = new()
        {
            Opcode = DlpOpcodes.WriteSysDateTime,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteSysDateTimeArguments.Request
            }
        };

        /************************************/
        /*  ReadStorageInfo                 */
        /************************************/
        public static class ReadStorageInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadStorageInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadStorageInfoMainResponse> MainResponse = new();
            public static readonly DlpArgumentDefinition<ReadStorageInfoExtResponse> ExtResponse = new();
        }
        
        public static readonly DlpCommandDefinition ReadStorageInfo = new()
        {
            Opcode = DlpOpcodes.ReadStorageInfo,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadStorageInfoArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadStorageInfoArguments.MainResponse,
                ReadStorageInfoArguments.ExtResponse
            }
        };

        /************************************/
        /*  ReadDbList                      */
        /************************************/
        public static class ReadDbListArguments
        {
            public static readonly DlpArgumentDefinition<ReadDbListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadDbListResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadDbList = new()
        {
            Opcode = DlpOpcodes.ReadDbList,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadDbListArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadDbListArguments.Response
            }
        };

        /************************************/
        /*  OpenDb                          */
        /************************************/
        public static class OpenDbArguments
        {
            public static readonly DlpArgumentDefinition<OpenDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<OpenDbResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition OpenDb = new()
        {
            Opcode = DlpOpcodes.OpenDb,
            RequestArguments = new DlpArgumentDefinition[]
            {
                OpenDbArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                OpenDbArguments.Response
            }
        };

        /************************************/
        /*  CreateDb                        */
        /************************************/
        public static class CreateDbArguments
        {
            public static readonly DlpArgumentDefinition<CreateDbRequest> Request = new();
            public static readonly DlpArgumentDefinition<CreateDbResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition CreateDb = new()
        {
            Opcode = DlpOpcodes.CreateDb,
            RequestArguments = new DlpArgumentDefinition[]
            {
                CreateDbArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                CreateDbArguments.Response
            }
        };

        /************************************/
        /*  CloseDb                         */
        /************************************/
        public static class CloseDbArguments
        {
            public static readonly DlpArgumentDefinition<CloseDbRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition CloseDb = new()
        {
            Opcode = DlpOpcodes.CloseDb,
            RequestArguments = new DlpArgumentDefinition[]
            {
                CloseDbArguments.Request
            }
        };

        /************************************/
        /*  DeleteDb                        */
        /************************************/
        public static class DeleteDbArguments
        {
            public static readonly DlpArgumentDefinition<DeleteDbRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition DeleteDb = new()
        {
            Opcode = DlpOpcodes.DeleteDb,
            RequestArguments = new DlpArgumentDefinition[]
            {
                DeleteDbArguments.Request
            }
        };

        /************************************/
        /*  ReadAppBlock                    */
        /************************************/
        public static class ReadAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadAppBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadAppBlockResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadAppBlock = new()
        {
            Opcode = DlpOpcodes.ReadAppBlock,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadAppBlockArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadAppBlockArguments.Response
            }
        };

        /************************************/
        /*  WriteAppBlock                   */
        /************************************/
        public static class WriteAppBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteAppBlockRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition WriteAppBlock = new()
        {
            Opcode = DlpOpcodes.WriteAppBlock,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteAppBlockArguments.Request
            }
        };

        /************************************/
        /* ReadSortBlock                    */
        /************************************/
        public static class ReadSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<ReadSortBlockRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadSortBlockResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadSortBlock = new()
        {
            Opcode = DlpOpcodes.ReadSortBlock,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadSortBlockArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadSortBlockArguments.Response
            }
        };

        /************************************/
        /* WriteSortBlock                   */
        /************************************/
        public static class WriteSortBlockArguments
        {
            public static readonly DlpArgumentDefinition<WriteSortBlockRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition WriteSortBlock = new()
        {
            Opcode = DlpOpcodes.WriteSortBlock,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteSortBlockArguments.Request
            }
        };

        /************************************/
        /* ReadNextModifiedRecord           */
        /************************************/
        public static class ReadNextModifiedRecordArguments
        {
            public static readonly DlpArgumentDefinition<ReadNextModifiedRecordRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadNextModifiedRecordResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadNextModifiedRecord = new()
        {
            Opcode = DlpOpcodes.ReadNextModifiedRecord,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadNextModifiedRecordArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadNextModifiedRecordArguments.Response
            }
        };

        /************************************/
        /* ReadRecord                       */
        /************************************/
        public static class ReadRecordArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordByIdRequest> RequestById = new();
            public static readonly DlpArgumentDefinition<ReadRecordByIndexRequest> RequestByIndex = new();
            public static readonly DlpArgumentDefinition<ReadRecordResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadRecord = new()
        {
            Opcode = DlpOpcodes.ReadRecord,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadRecordArguments.RequestById,
                ReadRecordArguments.RequestByIndex
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadRecordArguments.Response
            }
        };

        /************************************/
        /* WriteRecord                      */
        /************************************/
        public static class WriteRecordArguments
        {
            public static readonly DlpArgumentDefinition<WriteRecordRequest> Request = new();
            public static readonly DlpArgumentDefinition<WriteRecordResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition WriteRecord = new()
        {
            Opcode = DlpOpcodes.WriteRecord,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteRecordArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                WriteRecordArguments.Response
            }
        };

        /************************************/
        /* DeleteRecord                     */
        /************************************/
        public static class DeleteRecordArguments
        {
            public static readonly DlpArgumentDefinition<DeleteRecordRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition DeleteRecord = new()
        {
            Opcode = DlpOpcodes.DeleteRecord,
            RequestArguments = new DlpArgumentDefinition[]
            {
                DeleteRecordArguments.Request
            }
        };
        
        /************************************/
        /* ReadResource                     */
        /************************************/
        public static class ReadResourceArguments
        {
            public static readonly DlpArgumentDefinition<ReadResourceByIndexRequest> RequestByIndex = new();
            public static readonly DlpArgumentDefinition<ReadResourceResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadResource = new()
        {
            Opcode = DlpOpcodes.ReadResource,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadResourceArguments.RequestByIndex
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadResourceArguments.Response
            }
        };

        /************************************/
        /* WriteResource                    */
        /************************************/
        public static class WriteResourceArguments
        {
            public static readonly DlpArgumentDefinition<WriteResourceRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition WriteResource = new()
        {
            Opcode = DlpOpcodes.WriteResource,
            RequestArguments = new DlpArgumentDefinition[]
            {
                WriteResourceArguments.Request
            }
        };

        /************************************/
        /* DeleteResource                   */
        /**************************************/
        public static class DeleteResourceArguments
        {
            public static readonly DlpArgumentDefinition<DeleteResourceRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition DeleteResource = new()
        {
            Opcode = DlpOpcodes.DeleteResource,
            RequestArguments = new DlpArgumentDefinition[]
            {
                DeleteResourceArguments.Request
            }
        };

        /************************************/
        /* CleanUpDatabase                  */
        /************************************/
        public static class CleanUpDatabaseArguments
        {
            public static readonly DlpArgumentDefinition<CleanUpDatabaseRequest> Request = new();
        }

        public static readonly DlpCommandDefinition CleanUpDatabase = new()
        {
            Opcode = DlpOpcodes.CleanUpDatabase,
            RequestArguments = new DlpArgumentDefinition[]
            {
                CleanUpDatabaseArguments.Request
            }
        };

        /************************************/
        /* ResetSyncFlags                   */
        /************************************/
        public static class ResetSyncFlagsArguments
        {
            public static readonly DlpArgumentDefinition<ResetSyncFlagsRequest> Request = new();
        }

        public static readonly DlpCommandDefinition ResetSyncFlags = new()
        {
            Opcode = DlpOpcodes.ResetSyncFlags,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ResetSyncFlagsArguments.Request
            }
        };

        /************************************/
        /* CallApplication                  */
        /************************************/
        public static class CallApplicationArguments
        {
            public static readonly DlpArgumentDefinition<CallApplicationRequest> Request = new();
            public static readonly DlpArgumentDefinition<CallApplicationResponse> Response = new();
        }

        public static readonly DlpCommandDefinition CallApplication = new()
        {
            Opcode = DlpOpcodes.CallApplication,
            RequestArguments = new DlpArgumentDefinition[]
            {
                CallApplicationArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                CallApplicationArguments.Response
            }
        };

        /************************************/
        /* ResetSystem                      */
        /************************************/
        public static readonly DlpCommandDefinition ResetSystem = new()
        {
            Opcode = DlpOpcodes.ResetSystem
        };

        /************************************/
        /* AddSyncLogEntry                  */
        /************************************/
        public static class AddSyncLogEntryArguments
        {
            public static readonly DlpArgumentDefinition<AddSyncLogEntryRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition AddSyncLogEntry = new()
        {
            Opcode = DlpOpcodes.AddSyncLogEntry,
            RequestArguments = new DlpArgumentDefinition[]
            {
                AddSyncLogEntryArguments.Request
            }
        };
        
        /************************************/
        /* ReadOpenDbInfo                   */
        /************************************/
        public static class ReadOpenDbInfoArguments
        {
            public static readonly DlpArgumentDefinition<ReadOpenDbInfoRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadOpenDbInfoResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadOpenDbInfo = new()
        {
            Opcode = DlpOpcodes.ReadOpenDbInfo,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadOpenDbInfoArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadOpenDbInfoArguments.Response
            }
        };

        /************************************/
        /* MoveCategory                     */
        /************************************/
        public static class MoveCategoryArguments
        {
            public static readonly DlpArgumentDefinition<MoveCategoryRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition MoveCategory = new()
        {
            Opcode = DlpOpcodes.MoveCategory,
            RequestArguments = new DlpArgumentDefinition[]
            {
                MoveCategoryArguments.Request
            }
        };

        /************************************/
        /* OpenConduit                      */
        /************************************/
        public static readonly DlpCommandDefinition OpenConduit = new()
        {
            Opcode = DlpOpcodes.OpenConduit
        };

        /************************************/
        /* EndSync                          */
        /************************************/
        public static class EndSyncArguments
        {
            public static readonly DlpArgumentDefinition<EndSyncRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition EndSync = new()
        {
            Opcode = DlpOpcodes.EndSync,
            RequestArguments = new DlpArgumentDefinition[]
            {
                EndSyncArguments.Request
            }
        };

        /************************************/
        /* ResetRecordIndex                 */
        /************************************/
        public static class ResetRecordIndexArguments
        {
            public static readonly DlpArgumentDefinition<ResetRecordIndexRequest> Request = new();
        }
        
        public static readonly DlpCommandDefinition ResetRecordIndex = new()
        {
            Opcode = DlpOpcodes.ResetRecordIndex,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ResetRecordIndexArguments.Request
            }
        };

        /************************************/
        /* ReadRecordIdList                 */
        /************************************/
        public static class ReadRecordIdListArguments
        {
            public static readonly DlpArgumentDefinition<ReadRecordIdListRequest> Request = new();
            public static readonly DlpArgumentDefinition<ReadRecordIdListResponse> Response = new();
        }
        
        public static readonly DlpCommandDefinition ReadRecordIdList = new()
        {
            Opcode = DlpOpcodes.ReadRecordIdList,
            RequestArguments = new DlpArgumentDefinition[]
            {
                ReadRecordIdListArguments.Request
            },
            ResponseArguments = new DlpArgumentDefinition[]
            {
                ReadRecordIdListArguments.Response
            }
        };
    }
}
