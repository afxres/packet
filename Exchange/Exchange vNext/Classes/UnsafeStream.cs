using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal sealed class UnsafeStream
    {
        internal static MethodInfo AppendExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(AppendExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static MethodInfo AnchorExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(AnchorExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static MethodInfo FinishExtendMethodInfo { get; } = typeof(UnsafeStream).GetMethod(nameof(FinishExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;
        private byte[] buffer = new byte[InitialLength];
        private int position = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private byte[] ReAllocate(int offset, int require)
        {
            if ((uint)require > MaximumLength)
                ThrowHelper.ThrowOverflow();
            var source = buffer;
            var limits = (long)(offset + require);
            var length = (long)(source.Length);
            do
            {
                length <<= 2;
                if (length > MaximumLength)
                    ThrowHelper.ThrowOverflow();
            }
            while (length < limits);
            var target = new byte[(int)length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref source[0], (uint)offset);
            buffer = target;
            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Allocate(int require, out byte[] target)
        {
            var offset = position;
            target = buffer;
            if ((uint)require > (uint)(target.Length - offset))
                target = ReAllocate(offset, require);
            position = offset + require;
            return offset;
        }

        internal Block Allocate(int require)
        {
            var offset = Allocate(require, out var target);
            return new Block(target, offset, require);
        }

        internal int AnchorExtend() => Allocate(sizeof(int), out var _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FinishExtend(int offset) => UnmanagedValueConverter<int>.UnsafeToBytes(ref buffer[offset], position - offset - sizeof(int));

        internal byte[] ToArray() => new Block(buffer, 0, position).ToArray();

        internal void AppendExtend(byte[] source)
        {
            var length = source.Length;
            var offset = Allocate(length + sizeof(int), out var target);
            UnmanagedValueConverter<int>.UnsafeToBytes(ref target[offset], length);
            Unsafe.CopyBlockUnaligned(ref target[offset + sizeof(int)], ref source[0], (uint)length);
        }

        internal void Append(byte[] source)
        {
            int length;
            if (source == null || (length = source.Length) == 0)
                return;
            var offset = Allocate(length, out var target);
            Unsafe.CopyBlockUnaligned(ref target[offset], ref source[0], (uint)length);
        }

        internal void Append(string text)
        {
            int length;
            if (text == null || (length = text.Length) == 0)
                return;
            var encoding = Converter.Encoding;
            var offset = position;
            var cursor = 0;
            var single = Math.Max((length >> 3) + 1, 32);
            do
            {
                single = Math.Min(single, length - cursor);
                var maxCount = encoding.GetMaxByteCount(single);
                var target = buffer;
                if ((uint)maxCount > (uint)(target.Length - offset))
                    target = ReAllocate(offset, maxCount);
                offset += encoding.GetBytes(text, cursor, single, target, offset);
                cursor += single;
            }
            while (cursor != length);
            position = offset;
        }
    }
}
