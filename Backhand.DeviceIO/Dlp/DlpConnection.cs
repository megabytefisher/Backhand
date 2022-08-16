using System;
using Backhand.DeviceIO.DlpTransports;
using Backhand.Utility.Buffers;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.DeviceIO.Dlp
{
    public class DlpConnection
    {
        [Flags]
        private enum DlpArgType : byte
        {
            Tiny        = 0b00000000,
            Small       = 0b10000000,
            Long        = 0b01000000,
            All         = 0b11000000,
        }

        private readonly IDlpTransport _transport;

        private const int DlpRequestHeaderSize = sizeof(byte) * 2;
        private const int DlpResponseHeaderSize = sizeof(byte) * 4;
        private const int DlpResponseIdBitmask = 0x80;
        private const int DlpArgIdBase = 0x20;
        private const int DlpArgTinyMaxSize = byte.MaxValue;
        private const int DlpArgTinyHeaderSize = sizeof(byte) * 2;
        private const int DlpArgSmallMaxSize = ushort.MaxValue;
        private const int DlpArgSmallHeaderSize = sizeof(byte) * 4;

        //private static readonly TimeSpan DlpCommandResponseTimeout = TimeSpan.FromSeconds(30);

        public DlpConnection(IDlpTransport transport)
        {
            _transport = transport;
        }

        public async Task<DlpArgumentCollection> Execute(DlpCommandDefinition command, DlpArgumentCollection requestArguments, CancellationToken cancellationToken = default)
        {
            DlpArgumentCollection? responseArgs = null;
            
            byte[] requestBuffer = new byte[GetRequestSize(requestArguments)];
            WriteDlpRequest(command, requestArguments, requestBuffer);

            await _transport.ExecuteTransactionAsync(
                new DlpPayload(new ReadOnlySequence<byte>(requestBuffer)),
                (responsePayload) =>
                {
                    responseArgs = ReadDlpResponse(command, responsePayload.Buffer);
                },
                cancellationToken).ConfigureAwait(false);

            if (responseArgs == null)
                throw new DlpException("Didn't get response arguments back from transport");

            return responseArgs;
        }

        private static int GetRequestSize(DlpArgumentCollection arguments)
        {
            int size = DlpRequestHeaderSize;
            foreach (DlpArgument argument in arguments.GetValues())
            {
                int argumentSize = argument.GetSerializedLength();
                size += argumentSize switch
                {
                    <= DlpArgTinyMaxSize => DlpArgTinyHeaderSize + argumentSize,
                    <= DlpArgSmallMaxSize => DlpArgSmallHeaderSize + argumentSize,
                    _ => throw new DlpException("Request argument too big")
                };
            }
            return size;
        }

        private static void WriteDlpRequest(DlpCommandDefinition command, DlpArgumentCollection arguments, Span<byte> buffer)
        {
            int offset = 0;
            buffer[offset++] = (byte)command.Opcode;
            buffer[offset++] = Convert.ToByte(arguments.Count);

            for (int i = 0; i < command.RequestArguments.Length; i++)
            {
                DlpArgument? argument = arguments.GetValue(command.RequestArguments[i]);

                if (argument == null)
                {
                    if (!command.RequestArguments[i].IsOptional)
                        throw new DlpException("Missing required request argument");

                    continue;
                }

                int argumentSize = argument.GetSerializedLength();

                switch (argumentSize)
                {
                    case <= DlpArgTinyMaxSize:
                        buffer[offset++] = (byte)((DlpArgIdBase + i) | (int)DlpArgType.Tiny);
                        buffer[offset++] = Convert.ToByte(argumentSize);
                        break;
                    case <= DlpArgSmallMaxSize:
                        buffer[offset++] = (byte)((DlpArgIdBase + i) | (int)DlpArgType.Small);
                        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 1, 2), Convert.ToUInt16(argumentSize));
                        offset += 3;
                        break;
                    default:
                        throw new DlpException("Request argument too big");
                }

                Span<byte> argumentBuffer = buffer.Slice(offset, argumentSize);
                argument.Serialize(argumentBuffer);
            }
        }

        private static DlpArgumentCollection ReadDlpResponse(DlpCommandDefinition command, ReadOnlySequence<byte> buffer)
        {
            DlpArgumentCollection result = new();
            SequenceReader<byte> bufferReader = new(buffer);

            DlpOpcode opcode = (DlpOpcode)(bufferReader.Read() & ~DlpResponseIdBitmask);
            byte argCount = bufferReader.Read();

            if (opcode != command.Opcode)
                throw new DlpException("Received unexpected response opcode");

            DlpErrorCode errorCode = (DlpErrorCode)bufferReader.ReadUInt16BigEndian();
            if (errorCode != DlpErrorCode.Okay)
                throw new DlpCommandErrorException(errorCode);

            for (int i = 0; i < argCount; i++)
            {
                byte argIdWithType = bufferReader.Read();
                DlpArgType argType = (DlpArgType)(argIdWithType & (int)DlpArgType.All);
                int argId = argIdWithType & ~(int)DlpArgType.All;
                int argIndex = argId - DlpArgIdBase;

                int argLength;
                if (argType == DlpArgType.Tiny)
                {
                    argLength = bufferReader.Read();
                }
                else if (argType == DlpArgType.Small)
                {
                    bufferReader.Advance(1); // Ignore padding byte
                    argLength = bufferReader.ReadUInt16BigEndian();
                }
                else
                {
                    throw new DlpException("Response contained unsupported arg type");
                }

                ReadOnlySequence<byte> argBuffer = buffer.Slice(bufferReader.Position, argLength);
                bufferReader.Advance(argLength);

                DlpArgumentDefinition argDefinition = command.ResponseArguments[argIndex];
                DlpArgument argument = argDefinition.Deserialize(argBuffer);

                result.SetValue(argDefinition, argument);
            }

            return result;
        }
    }
}
