﻿using System;
using System.Buffers;
using Backhand.Common.Buffers;

namespace Backhand.Common.Checksums
{
    public static class Crc16
    {
        public static ushort ComputeChecksum(Span<byte> bytes)
        {
            int crc = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                crc ^= bytes[i] << 8;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (crc << 1) ^ 0x1021;
                    }
                    else
                    {
                        crc = crc << 1;
                    }
                }
            }

            return (ushort)crc;
        }

        public static ushort ComputeChecksum(ReadOnlySequence<byte> bytes)
        {
            SequenceReader<byte> bytesReader = new(bytes);

            int crc = 0;

            while (!bytesReader.End)
            {
                byte value = bytesReader.Read();

                crc ^= value << 8;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (crc << 1) ^ 0x1021;
                    }
                    else
                    {
                        crc = crc << 1;
                    }
                }
            }

            return (ushort)crc;
        }
    }
}
