using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    internal sealed class UnsafeStream
    {
        private const int InitialLength = 256;
        private static readonly bool ReverseEndianness = BitConverter.IsLittleEndian != PacketConvert.UseLittleEndian;

        private byte[] stream = new byte[InitialLength];
        private int position = 0;

        private int VerifyAvailable(int require)
        {
            var offset = position;
            var buffer = stream;
            var limits = offset + require;
            if (limits <= buffer.Length)
                goto end;
            var value = (long)buffer.Length;
            while (true)
            {
                value <<= 2;
                if (value > 0x4000_0000L)
                    throw PacketException.Overflow();
                if (limits <= value)
                    break;
            }
            var result = new byte[(int)value];
            Unsafe.CopyBlockUnaligned(ref result[0], ref buffer[0], (uint)offset);
            stream = result;

            end:
            position = limits;
            return offset;
        }

        private void WriteHeader(int offset, int value) => Unsafe.WriteUnaligned(ref stream[offset], (!ReverseEndianness) ? (uint)value : Extension.ReverseEndianness((uint)value));

        internal void Write(byte[] buffer)
        {
            var offset = VerifyAvailable(buffer.Length);
            Unsafe.CopyBlockUnaligned(ref stream[offset], ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteExtend(UnsafeStream other)
        {
            var length = other.position;
            var offset = VerifyAvailable(length + sizeof(int));
            WriteHeader(offset, length);
            if (length < 1)
                return;
            Unsafe.CopyBlockUnaligned(ref stream[offset + sizeof(int)], ref other.stream[0], (uint)length);
        }

        internal void WriteExtend(byte[] buffer)
        {
            var offset = VerifyAvailable(buffer.Length + sizeof(int));
            WriteHeader(offset, buffer.Length);
            if (buffer.Length < 1)
                return;
            Unsafe.CopyBlockUnaligned(ref stream[offset + sizeof(int)], ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteKey(string key) => WriteExtend(Extension.Encoding.GetBytes(key));

        internal int BeginModify() => VerifyAvailable(sizeof(int));

        internal void EndModify(int offset) => WriteHeader(offset, (position - offset - sizeof(int)));

        internal int GetPosition() => position;

        internal byte[] GetBytes()
        {
            var offset = position;
            var result = new byte[offset];
            if (offset > 0)
                Unsafe.CopyBlockUnaligned(ref result[0], ref stream[0], (uint)offset);
            return result;
        }
    }
}
