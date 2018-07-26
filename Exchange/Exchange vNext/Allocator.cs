using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public sealed class Allocator
    {
        #region non-public
        internal static MethodInfo AppendExtendMethodInfo { get; } = typeof(Allocator).GetMethod(nameof(AppendExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static MethodInfo AnchorExtendMethodInfo { get; } = typeof(Allocator).GetMethod(nameof(AnchorExtend), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static MethodInfo FinishExtendMethodInfo { get; } = typeof(Allocator).GetMethod(nameof(FinishExtend), BindingFlags.Instance | BindingFlags.NonPublic);

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
        private int Allocate(int require, out byte[] target)
        {
            var offset = position;
            target = buffer;
            if ((uint)require > (uint)(target.Length - offset))
                target = ReAllocate(offset, require);
            position = offset + require;
            return offset;
        }

        internal int AnchorExtend() => Allocate(sizeof(int), out var _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FinishExtend(int offset) => UnmanagedValueConverter<int>.UnsafeToBytes(ref buffer[offset], position - offset - sizeof(int));

        internal byte[] ToArray() => new ReadOnlyMemory<byte>(buffer, 0, position).ToArray();

        internal void AppendExtend(byte[] source)
        {
            var length = source.Length;
            var offset = Allocate(length + sizeof(int), out var target);
            UnmanagedValueConverter<int>.UnsafeToBytes(ref target[offset], length);
            Unsafe.Copy(ref target[offset + sizeof(int)], in source[0], length);
        }
        #endregion

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

        public Memory<byte> Allocate(int length)
        {
            var offset = Allocate(length, out var target);
            return new Memory<byte>(target, offset, length);
        }

        public void Append(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
                return;
            var offset = Allocate(span.Length, out var target);
            Unsafe.Copy(ref target[offset], in span[0], span.Length);
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
