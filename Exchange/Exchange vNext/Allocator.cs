using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public sealed class Allocator
    {
        #region non-public
        internal static readonly MethodInfo AppendExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AppendExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly MethodInfo AnchorExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AnchorExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly MethodInfo FinishExtendMethodInfo = typeof(Allocator).GetMethod(nameof(FinishExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;
        private byte[] buffer = new byte[InitialLength];
        private int position = 0;

        internal Allocator() { }

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
            Unsafe.Copy(ref target[0], in source[0], offset);
            buffer = target;
            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int AnchorExtend()
        {
            var offset = position;
            var target = buffer;
            if (sizeof(int) > (uint)(target.Length - offset))
                target = ReAllocate(offset, sizeof(int));
            position = offset + sizeof(int);
            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void FinishExtend(int offset)
        {
            const int cursor = sizeof(int) - 1;
            ref var target = ref buffer[offset + cursor];
            fixed (byte* pointer = &target)
                UnmanagedValueConverter<int>.UnsafeToBytes(pointer - cursor, position - offset - sizeof(int));
        }

        internal unsafe void AppendExtend(byte[] source)
        {
            var length = source.Length;
            ref var target = ref Allocate(length + sizeof(int));
            fixed (byte* src = &source[0])
            fixed (byte* dst = &target)
            {
                UnmanagedValueConverter<int>.UnsafeToBytes(dst, length);
                Unsafe.Copy(dst + sizeof(int), src, length);
            }
        }

        internal byte[] ToArray() => new ReadOnlySpan<byte>(buffer, 0, position).ToArray();
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> AllocateMemory(int length)
        {
            var offset = position;
            var target = buffer;
            if ((uint)length > (uint)(target.Length - offset))
                target = ReAllocate(offset, length);
            position = offset + length;
            return new Memory<byte>(target, offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref byte Allocate(int length)
        {
            var offset = position;
            var target = buffer;
            if ((uint)length > (uint)(target.Length - offset))
                target = ReAllocate(offset, length);
            position = offset + length;
            return ref target[offset];
        }

        public unsafe void Append(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
                return;
            var encoding = Converter.Encoding;
            var offset = position;
            var cursor = 0;
            var charCount = Math.Max((span.Length >> 3) + 1, 32);
            do
            {
                charCount = Math.Min(charCount, span.Length - cursor);
                var byteCount = encoding.GetMaxByteCount(charCount);
                var target = buffer;
                if ((uint)byteCount > (uint)(target.Length - offset))
                    target = ReAllocate(offset, byteCount);
                fixed (char* chars = &span[cursor])
                fixed (byte* bytes = &target[offset])
                    offset += encoding.GetBytes(chars, charCount, bytes, byteCount);
                cursor += charCount;
            }
            while (cursor != span.Length);
            position = offset;
        }

        public void Append(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
                return;
            ref var target = ref Allocate(span.Length);
            Unsafe.Copy(ref target, in span[0], span.Length);
        }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => nameof(Allocator);
        #endregion
    }
}
