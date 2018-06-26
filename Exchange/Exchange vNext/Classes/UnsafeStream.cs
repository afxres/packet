using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal sealed class UnsafeStream
    {
        internal static MethodInfo WriteExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(WriteExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static MethodInfo BeginModifyMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(BeginModify), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static MethodInfo EndModifyMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(EndModify), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;

        internal byte[] stream = new byte[InitialLength];
        internal int position;

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

        internal int VerifyAvailable(int require)
        {
            if ((uint)require > MaximumLength)
                throw new OverflowException();
            var offset = position;
            if (stream.Length - offset < require)
                ReAllocate(require, offset);
            position = offset + require;
            return offset;
        }

        internal UnsafeStream() { }

        internal void WriteExtend(byte[] buffer)
        {
            var offset = VerifyAvailable(buffer.Length + sizeof(int));
            UnmanagedValueConverter<int>.BytesUnchecked(ref stream[offset], buffer.Length);
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
            UnmanagedValueConverter<int>.BytesUnchecked(ref buffer[offset], position - offset - sizeof(int));
        }

        internal byte[] GetBytes()
        {
            var length = position;
            if (length == 0)
                return Empty.Array<byte>();
            var buffer = stream;
            var target = new byte[length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[0], (uint)length);
            return target;
        }
    }
}
