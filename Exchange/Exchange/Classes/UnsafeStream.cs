using Mikodev.Network.Converters;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    internal sealed class UnsafeStream
    {
        /* 警告: 该类仅应用于单线程环境 */

        private const int InitialLength = 256;

        private byte[] stream = new byte[InitialLength];
        private int position;

        private int VerifyAvailable(int require)
        {
            var offset = position;
            long limits = offset + (uint)require;
            long length = stream.Length;
            if (length < limits)
            {
                do
                {
                    length <<= 2;
                    if (length > 0x4000_0000L)
                        throw PacketException.Overflow();
                }
                while (length < limits);
                var target = new byte[(int)length];
                Unsafe.CopyBlockUnaligned(ref target[0], ref stream[0], (uint)offset);
                stream = target;
            }
            position = (int)limits;
            return offset;
        }

        internal void Write(byte[] buffer)
        {
            if (buffer.Length == 0)
                return;
            var offset = VerifyAvailable(buffer.Length);
            Unsafe.CopyBlockUnaligned(ref stream[offset], ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteExtend(byte[] buffer)
        {
            var offset = VerifyAvailable(buffer.Length + sizeof(int));
            UnmanagedValueConverter<int>.ToBytesUnchecked(ref stream[offset], buffer.Length);
            if (buffer.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref stream[offset + sizeof(int)], ref buffer[0], (uint)buffer.Length);
        }

        internal void WriteKey(string key) => WriteExtend(Extension.Encoding.GetBytes(key));

        internal int BeginModify() => VerifyAvailable(sizeof(int));

        internal void EndModify(int offset) => UnmanagedValueConverter<int>.ToBytesUnchecked(ref stream[offset], (position - offset - sizeof(int)));

        internal byte[] GetBytes()
        {
            var length = position;
            var target = new byte[length];
            if (length > 0)
                Unsafe.CopyBlockUnaligned(ref target[0], ref stream[0], (uint)length);
            return target;
        }
    }
}
