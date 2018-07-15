using Mikodev.Network.Converters;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    internal sealed class UnsafeStream
    {
        /* 警告: 该类仅应用于单线程环境 */

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;

        private byte[] buffer = new byte[InitialLength];
        private int position = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReAllocate(int offset, int require)
        {
            if ((uint)require > MaximumLength)
                goto fail;
            long limits = offset + require;
            long length = buffer.Length;
            do
            {
                length <<= 2;
                if (length > MaximumLength)
                    goto fail;
            }
            while (length < limits);
            var target = new byte[(int)length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[0], (uint)offset);
            buffer = target;
            return;

            fail:
            throw PacketException.Overflow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Allocate(int require)
        {
            var offset = position;
            if ((uint)require > (uint)(buffer.Length - offset))
                ReAllocate(offset, require);
            position = offset + require;
            return offset;
        }

        internal void Append(byte[] source)
        {
            if (source.Length == 0)
                return;
            var offset = Allocate(source.Length);
            Unsafe.CopyBlockUnaligned(ref buffer[offset], ref source[0], (uint)source.Length);
        }

        internal void AppendExtend(byte[] source)
        {
            var offset = Allocate(source.Length + sizeof(int));
            UnmanagedValueConverter<int>.UnsafeToBytes(ref buffer[offset], source.Length);
            if (source.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref buffer[offset + sizeof(int)], ref source[0], (uint)source.Length);
        }

        internal void AppendKey(string key) => AppendExtend(PacketConvert.Encoding.GetBytes(key));

        internal int AnchorExtend() => Allocate(sizeof(int));

        internal void FinishExtend(int offset) => UnmanagedValueConverter<int>.UnsafeToBytes(ref buffer[offset], (position - offset - sizeof(int)));

        internal byte[] GetBytes()
        {
            var length = position;
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[0], (uint)length);
            return target;
        }
    }
}
