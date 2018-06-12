using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mikodev.Binary
{
    public sealed class Allocator
    {
        #region private
        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;

        private byte[] stream = new byte[InitialLength];
        private int position;

        private void ReAllocate(int require, int offset)
        {
            long limits = offset + require;
            long length = stream.Length;
            do
            {
                length <<= 2;
                if (length > MaximumLength)
                    throw new OverflowException();
            }
            while (length < limits);
            var target = new byte[(int)length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref stream[0], (uint)offset);
            stream = target;
        }

        private int VerifyAvailable(int require)
        {
            if ((uint)require > MaximumLength)
                throw new OverflowException();
            var offset = position;
            if (stream.Length - offset < require)
                ReAllocate(require, offset);
            position = offset + require;
            return offset;
        }
        #endregion

        #region internal
        internal Allocator() { }

        internal void WriteExtend(byte[] buffer)
        {
            var offset = VerifyAvailable(buffer.Length + sizeof(int));
            Unsafe.WriteUnaligned(ref stream[offset], buffer.Length);
            if (buffer.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref stream[offset + sizeof(int)], ref buffer[0], (uint)buffer.Length);
        }

        internal int BeginModify() => VerifyAvailable(sizeof(int));

        internal void EndModify(int offset)
        {
            var buffer = stream;
            if (buffer.Length - offset < sizeof(int))
                throw new ArgumentOutOfRangeException();
            Unsafe.WriteUnaligned(ref buffer[offset], position - offset - sizeof(int));
        }

        internal byte[] GetBytes()
        {
            var offset = Volatile.Read(ref position);
            if (offset == 0)
                return Array.Empty<byte>();
            var buffer = stream;
            var target = new byte[offset];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[0], (uint)offset);
            return target;
        }
        #endregion

        public Span<byte> Allocate(int length)
        {
            var offset = VerifyAvailable(length);
            return new Span<byte>(stream, offset, length);
        }
    }
}
