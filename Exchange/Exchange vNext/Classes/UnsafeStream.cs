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

        internal byte[] buffer = new byte[InitialLength];
        internal int position;

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
            throw new OverflowException("Data length overflow!");
        }

        internal int VerifyAvailable(int require)
        {
            var offset = position;
            if ((uint)require > MaximumLength || buffer.Length - offset < require)
                ReAllocate(offset, require);
            position = offset + require;
            return offset;
        }

        internal UnsafeStream() { }

        internal void WriteExtend(byte[] source)
        {
            var offset = VerifyAvailable(source.Length + sizeof(int));
            UnmanagedValueConverter<int>.BytesUnchecked(ref buffer[offset], source.Length);
            if (source.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref buffer[offset + sizeof(int)], ref source[0], (uint)source.Length);
        }

        internal int BeginModify() => VerifyAvailable(sizeof(int));

        internal void EndModify(int offset)
        {
            var target = buffer;
            if (target.Length - offset < sizeof(int))
                throw new ArgumentOutOfRangeException();
            UnmanagedValueConverter<int>.BytesUnchecked(ref target[offset], position - offset - sizeof(int));
        }

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
