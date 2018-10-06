using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public sealed class Allocator
    {
        #region non-public
        internal static readonly MethodInfo AppendBytesExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AppendBytesExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static readonly MethodInfo AppendValueExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AppendValueExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;

        private const int MaximumLength = 0x4000_0000;

        private byte[] buffer = new byte[InitialLength];

        private int position = 0;

        internal Allocator() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe byte[] ReAllocate(int offset, int require)
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
            Unsafe.Copy(target, source, offset);
            buffer = target;
            return target;
        }

        internal unsafe void AppendValueExtend<T>(Converter<T> converter, T value)
        {
            var offset = position;
            var target = buffer;
            if (sizeof(int) > (uint)(target.Length - offset))
                target = ReAllocate(offset, sizeof(int));
            position = offset + sizeof(int);

            converter.ToBytes(this, value);

            const int cursor = sizeof(int) - 1;
            fixed (byte* dstptr = &buffer[offset + cursor])
                UnmanagedValueConverter<int>.UnsafeToBytes(dstptr - cursor, position - offset - sizeof(int));
        }

        internal unsafe void AppendBytesExtend(byte[] source)
        {
            var length = source.Length;
            fixed (byte* srcptr = &source[0])
            fixed (byte* dstptr = &Allocate(length + sizeof(int)))
            {
                UnmanagedValueConverter<int>.UnsafeToBytes(dstptr, length);
                Unsafe.Copy(dstptr + sizeof(int), srcptr, length);
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
            var limits = span.Length;
            var charCount = Math.Max((limits >> 3) + 1, 32);
            do
            {
                charCount = Math.Min(charCount, limits - cursor);
                var byteCount = encoding.GetMaxByteCount(charCount);
                var target = buffer;
                if ((uint)byteCount > (uint)(target.Length - offset))
                    target = ReAllocate(offset, byteCount);
                fixed (char* srcptr = &span[cursor])
                fixed (byte* dstptr = &target[offset])
                    offset += encoding.GetBytes(srcptr, charCount, dstptr, byteCount);
                cursor += charCount;
            }
            while (cursor != limits);
            position = offset;
        }

        public unsafe void Append(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
                return;
            var limits = span.Length;
            fixed (byte* dstptr = &Allocate(limits))
            fixed (byte* srcptr = span)
                Unsafe.Copy(dstptr, srcptr, limits);
        }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Allocator)}(Length: {position}, Capacity: {buffer.Length})";
        #endregion
    }
}
