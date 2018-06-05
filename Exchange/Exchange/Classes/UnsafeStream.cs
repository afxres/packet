using Mikodev.Network.Converters;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    internal sealed class UnsafeStream
    {
        private readonly struct VerifyResult
        {
            internal readonly byte[] buffer;
            internal readonly int offset;

            internal ref byte Head => ref buffer[offset];

            internal ref byte Tail => ref buffer[offset + sizeof(int)];

            internal VerifyResult(byte[] buffer, int offset)
            {
                this.buffer = buffer;
                this.offset = offset;
            }
        }

        private const int InitialLength = 256;

        private byte[] stream = new byte[InitialLength];
        private int position;

        private VerifyResult VerifyAvailable(int require)
        {
            var offset = position;
            var buffer = stream;
            var bound = offset + (uint)require;
            var value = (long)buffer.Length;
            if (bound <= value)
                goto end;
            while (true)
            {
                value <<= 2;
                if (value > 0x4000_0000L)
                    throw PacketException.Overflow();
                if (bound <= value)
                    break;
            }
            var result = new byte[(int)value];
            Unsafe.CopyBlockUnaligned(ref result[0], ref buffer[0], (uint)offset);
            stream = result;
            buffer = result;

            end:
            position = (int)bound;
            return new VerifyResult(buffer, offset);
        }

        internal void Write(byte[] buffer)
        {
            var result = VerifyAvailable(buffer.Length);
            Unsafe.CopyBlockUnaligned(ref result.Head, ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteExtend(UnsafeStream other)
        {
            var length = other.position;
            var result = VerifyAvailable(length + sizeof(int));
            UnmanagedValueConverter<int>.ToBytesUnchecked(ref result.Head, length);
            if (length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref result.Tail, ref other.stream[0], (uint)length);
        }

        internal void WriteExtend(byte[] buffer)
        {
            var result = VerifyAvailable(buffer.Length + sizeof(int));
            UnmanagedValueConverter<int>.ToBytesUnchecked(ref result.Head, buffer.Length);
            if (buffer.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref result.Tail, ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteKey(string key) => WriteExtend(Extension.Encoding.GetBytes(key));

        internal int BeginModify() => VerifyAvailable(sizeof(int)).offset;

        internal void EndModify(int offset) => UnmanagedValueConverter<int>.ToBytesUnchecked(ref stream[offset], (position - offset - sizeof(int)));

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
