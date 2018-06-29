using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal sealed class UnsafeStream
    {
        internal static MethodInfo AppendExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(AppendExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static MethodInfo BeginExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(BeginExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static MethodInfo EndExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(EndExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;

        internal byte[] buffer = new byte[InitialLength];
        internal int position = 0;

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
            if ((uint)require > (uint)(buffer.Length - offset))
                ReAllocate(offset, require);
            position = offset + require;
            return offset;
        }

        internal void AppendExtend(byte[] source)
        {
            var offset = VerifyAvailable(source.Length + sizeof(int));
            UnmanagedValueConverter<int>.ToBytesUnchecked(ref buffer[offset], source.Length);
            if (source.Length == 0)
                return;
            Unsafe.CopyBlockUnaligned(ref buffer[offset + sizeof(int)], ref source[0], (uint)source.Length);
        }

        internal void Append(byte[] source)
        {
            if (source is null || source.Length == 0)
                return;
            var offset = VerifyAvailable(source.Length);
            Unsafe.CopyBlockUnaligned(ref buffer[offset], ref source[0], (uint)source.Length);
        }

        internal void Append(string text)
        {
            int length;
            if (text is null || (length = text.Length) == 0)
                return;
            var encoding = Converter.Encoding;
            var offset = position;
            var cursor = 0;
            var single = Math.Max((length >> 3) + 1, 32);
            do
            {
                single = Math.Min(single, length - cursor);
                var maxCount = encoding.GetMaxByteCount(single);
                if ((uint)maxCount > (uint)(buffer.Length - offset))
                    ReAllocate(offset, maxCount);
                offset += encoding.GetBytes(text, cursor, single, buffer, offset);
                cursor += single;
            }
            while (cursor != length);
            position = offset;
        }

        internal int BeginExtend() => VerifyAvailable(sizeof(int));

        internal void EndExtend(int offset) => UnmanagedValueConverter<int>.ToBytesUnchecked(ref buffer[offset], position - offset - sizeof(int));

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
