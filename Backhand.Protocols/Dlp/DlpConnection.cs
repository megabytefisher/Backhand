using Backhand.Common.Buffers;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Backhand.Protocols.Dlp
{
    public class DlpConnection
    {
        [Flags]
        private enum DlpArgType : byte
        {
            Tiny = 0b00000000,
            Small = 0b10000000,
            Long = 0b01000000,

            Mask = 0b11000000
        }

        private readonly IDlpTransport _transport;
        private readonly ArrayPool<byte> _arrayPool;

        private const int DlpRequestHeaderSize = sizeof(byte) * 2;
        private const int DlpResponseHeaderSize = sizeof(byte) * 4;
        private const byte DlpResponseIdMask = 0x80;
        private const int DlpArgIdBase = 0x20;
        private const int DlpTinyArgMaxSize = byte.MaxValue;
        private const int DlpTinyArgHeaderSize = sizeof(byte) * 2;
        private const int DlpSmallArgMaxSize = ushort.MaxValue;
        private const int DlpSmallArgHeaderSize = sizeof(byte) * 4;

        public DlpConnection(IDlpTransport transport, ArrayPool<byte>? arrayPool = null)
        {
            _transport = transport;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
        }

        public async Task<DlpArgumentMap> ExecuteTransactionAsync(DlpCommandDefinition commandDefinition, DlpArgumentMap? requestArguments = null, CancellationToken cancellationToken = default)
        {
            requestArguments = requestArguments ?? new DlpArgumentMap();
            DlpArgumentMap? responseArguments = null;

            int requestSize = GetRequestSize(commandDefinition, requestArguments);
            byte[] requestBuffer = _arrayPool.Rent(requestSize);
            WriteDlpRequest(new Span<byte>(requestBuffer, 0, requestSize), commandDefinition, requestArguments);

            await _transport.ExecuteTransactionAsync(
                new ReadOnlySequence<byte>(requestBuffer, 0, requestSize),
                (responseData) =>
                {
                    responseArguments = ReadDlpResponse(commandDefinition, responseData);
                },
                cancellationToken).ConfigureAwait(false);

            if (responseArguments == null)
            {
                throw new DlpException("Didn't get response arguments back from transport");
            }

            return responseArguments;
        }

        private static int GetRequestSize(DlpCommandDefinition commandDefinition, DlpArgumentMap arguments)
        {
            int size = DlpRequestHeaderSize;
            foreach (DlpArgumentDefinition argumentDefinition in commandDefinition.RequestArguments)
            {
                DlpArgument? argument = arguments.GetValue(argumentDefinition);
                if (argument == null)
                {
                    if (argumentDefinition.IsOptional)
                    {
                        continue;
                    }
                    else
                    {
                        throw new DlpException("Non-optional DLP argument is missing");
                    }
                }

                int argumentSize = argumentDefinition.GetSerializedSize(argument);
                size += argumentSize switch
                {
                    <= DlpTinyArgMaxSize => DlpTinyArgHeaderSize,
                    <= DlpSmallArgMaxSize => DlpSmallArgHeaderSize,
                    _ => throw new DlpException("Request argument too big")
                };
                size += argumentSize;
            }
            return size;
        }

        private static void WriteDlpRequest(Span<byte> buffer, DlpCommandDefinition commandDefinition, DlpArgumentMap arguments)
        {
            SpanWriter<byte> bufferWriter = new(buffer);

            bufferWriter.Write((byte)commandDefinition.Opcode);
            bufferWriter.Write(Convert.ToByte(arguments.Count));

            for (int i = 0; i < commandDefinition.RequestArguments.Length; i++)
            {
                DlpArgumentDefinition argumentDefinition = commandDefinition.RequestArguments[i];

                DlpArgument? argument = arguments.GetValue(argumentDefinition);
                if (argument == null)
                {
                    if (argumentDefinition.IsOptional)
                    {
                        continue;
                    }
                    else
                    {
                        throw new DlpException("Non-optional DLP argument is missing");
                    }
                }

                int argumentSize = argumentDefinition.GetSerializedSize(argument);

                switch (argumentSize)
                {
                    case <= DlpTinyArgMaxSize:
                        bufferWriter.Write((byte)((DlpArgIdBase + i) | (int)DlpArgType.Tiny));
                        bufferWriter.Write(Convert.ToByte(argumentSize));
                        break;
                    case <= DlpSmallArgMaxSize:
                        bufferWriter.Write((byte)((DlpArgIdBase + i) | (int)DlpArgType.Small));
                        bufferWriter.Write((byte)0);
                        bufferWriter.WriteUInt16BigEndian(Convert.ToUInt16(argumentSize));
                        break;
                    default:
                        throw new DlpException($"Unexpected argument size: {argumentSize}");
                }

                argumentDefinition.Serialize(ref bufferWriter, argument);
            }
        }

        private static DlpArgumentMap ReadDlpResponse(DlpCommandDefinition commandDefinition, ReadOnlySequence<byte> buffer)
        {
            SequenceReader<byte> bufferReader = new(buffer);

            DlpArgumentMap results = new();

            byte opcode = (byte)(bufferReader.Read() & ~DlpResponseIdMask);
            byte argumentCount = bufferReader.Read();

            if (opcode != commandDefinition.Opcode)
            {
                throw new DlpException("Received unexpected response opcode");
            }

            // Error code
            DlpErrorCode errorCode = (DlpErrorCode)bufferReader.ReadUInt16BigEndian();
            if (errorCode != DlpErrorCode.Success)
            {
                throw new DlpCommandErrorException(errorCode);
            }

            for (int i = 0; i < argumentCount; i++)
            {
                byte argumentIdAndType = bufferReader.Read();
                DlpArgType argumentType = (DlpArgType)(argumentIdAndType & (int)DlpArgType.Mask);
                int argumentId = argumentIdAndType & ~(int)DlpArgType.Mask;
                int argumentIndex = argumentId - DlpArgIdBase;

                int argumentLength;
                switch (argumentType)
                {
                    case DlpArgType.Tiny:
                        argumentLength = bufferReader.Read();
                        break;
                    case DlpArgType.Small:
                        bufferReader.Advance(1);
                        argumentLength = bufferReader.ReadUInt16BigEndian();
                        break;
                    default:
                        throw new DlpException("Response contained unsupported argument type");
                }

                DlpArgumentDefinition argumentDefinition = commandDefinition.ResponseArguments[argumentIndex];
                DlpArgument argument = Activator.CreateInstance(argumentDefinition.Type) as DlpArgument ?? throw new Exception("Failed to instantiate argument type");
                argumentDefinition.Deserialize(ref bufferReader, argument);

                results.SetValue(argumentDefinition, argument);
            }

            return results;
        }
    }
}
