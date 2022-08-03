using Backhand.DeviceIO.Padp;
using Backhand.DeviceIO.Utility;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private PadpConnection _padp;

        private const int DlpRequestHeaderSize = 2;
        private const int DlpResponseHeaderSize = 4;
        private const int DlpResponseIdBitmask = 0x80;
        private const int DlpArgIdBase = 0x20;
        private const int DlpArgTinyMaxSize = byte.MaxValue;
        private const int DlpArgTinyHeaderSize = 2;
        private const int DlpArgSmallMaxSize = ushort.MaxValue;
        private const int DlpArgSmallHeaderSize = 4;

        public DlpConnection(PadpConnection padp)
        {
            _padp = padp;
        }

        public async Task<DlpArgumentCollection> Execute(DlpCommandDefinition command, DlpArgumentCollection requestArguments)
        {
            Task<DlpArgumentCollection> responseWaitTask = WaitForResponseAsync(command);

            byte[] requestBuffer = new byte[GetRequestSize(requestArguments)];
            WriteDlpRequest(command, requestArguments, requestBuffer);
            await _padp.SendData(requestBuffer);

            DlpArgumentCollection responseArguments = await responseWaitTask;
            _padp.BumpTransactionId();
            return responseArguments;
        }

        private int GetRequestSize(DlpArgumentCollection arguments)
        {
            int size = DlpRequestHeaderSize;
            foreach (DlpArgument argument in arguments.GetValues())
            {
                int argumentSize = argument.GetSerializedLength();
                if (argumentSize <= DlpArgTinyMaxSize)
                    size += DlpArgTinyHeaderSize + argumentSize;
                else if (argumentSize <= DlpArgSmallMaxSize)
                    size += DlpArgSmallHeaderSize + argumentSize;
                else
                    throw new DlpException("Request argument too big");
            }
            return size;
        }

        private void WriteDlpRequest(DlpCommandDefinition command, DlpArgumentCollection arguments, Span<byte> buffer)
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

                if (argumentSize <= DlpArgTinyMaxSize)
                {
                    buffer[offset++] = (byte)((DlpArgIdBase + i) | (int)DlpArgType.Tiny);
                    buffer[offset++] = Convert.ToByte(argumentSize);
                }
                else if (argumentSize <= DlpArgSmallMaxSize)
                {
                    buffer[offset++] = (byte)((DlpArgIdBase + i) | (int)DlpArgType.Small);
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 1, 2), Convert.ToUInt16(argumentSize));
                }
                else
                {
                    throw new DlpException("Request argument too big");
                }

                Span<byte> argumentBuffer = buffer.Slice(offset, argumentSize);
                argument.Serialize(argumentBuffer);
            }
        }

        private DlpArgumentCollection ReadDlpResponse(DlpCommandDefinition command, ReadOnlySequence<byte> buffer)
        {
            DlpArgumentCollection result = new DlpArgumentCollection();
            SequenceReader<byte> bufferReader = new SequenceReader<byte>(buffer);

            DlpOpcode opcode = (DlpOpcode)(bufferReader.Read() & ~DlpResponseIdBitmask);
            byte argCount = bufferReader.Read();

            if (opcode != command.Opcode)
                throw new DlpException("Got DLP response for different opcode");

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

        private async Task<DlpArgumentCollection> WaitForResponseAsync(DlpCommandDefinition command)
        {
            using AsyncFlag responseFlag = new AsyncFlag();
            Exception? exception = null;
            DlpArgumentCollection? result = null;

            Action<object?, PadpDataReceivedEventArgs> responseReceiver = (sender, e) =>
            {
                try
                {
                    result = ReadDlpResponse(command, e.Data);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                responseFlag.Set();
            };

            _padp.ReceivedData += responseReceiver.Invoke;
            await responseFlag.WaitAsync();
            _padp.ReceivedData -= responseReceiver.Invoke;

            if (exception != null)
                throw exception;

            return result!;
        }
    }
}
