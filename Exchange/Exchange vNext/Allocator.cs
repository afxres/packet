using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public ref struct Allocator
    {
        #region static or constant
        internal static readonly MethodInfo AppendBytesExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AppendBytesExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static readonly MethodInfo AppendValueExtendMethodInfo = typeof(Allocator).GetMethod(nameof(AppendValueExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int InitialLength = 256;

        private const int MaximumLength = 0x4000_0000;
        #endregion

        #region private fields
        private byte[] buffer;

        private int position;
        #endregion

        #region non-public methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe byte[] ReAllocate(int offset, int require)
        {
            if ((uint)require > MaximumLength)
                ThrowHelper.ThrowOverflow();
            var source = buffer;
            var limits = (long)(offset + require);
            var length = (long)(source?.Length ?? 0);
            if (length == 0)
                length = InitialLength;
            while (length < limits)
                if ((length <<= 2) > MaximumLength)
                    ThrowHelper.ThrowOverflow();
            var target = new byte[(int)length];
            if (offset != 0)
                Unsafe.Copy(target, source, offset);
            buffer = target;
            return target;
        }

        internal unsafe void AppendValueExtend<T>(Converter<T> converter, T value)
        {
            var offset = position;
            var target = buffer;
            if (target == null || sizeof(int) > (uint)(target.Length - offset))
                target = ReAllocate(offset, sizeof(int));
            var size = offset + sizeof(int);
            position = size;

            converter.ToBytes(ref this, value);

            target = buffer;
            if (target == null || target.Length < size)
                ThrowHelper.ThrowAllocatorModified();
            fixed (byte* dstptr = &target[offset])
                UnmanagedValueConverter<int>.UnsafeToBytes(dstptr, position - size);
        }

        internal unsafe void AppendBytesExtend(byte[] source)
        {
            var length = source.Length;
            fixed (byte* dstptr = Allocate(length + sizeof(int)))
            fixed (byte* srcptr = &source[0])
            {
                UnmanagedValueConverter<int>.UnsafeToBytes(dstptr, length);
                Unsafe.Copy(dstptr + sizeof(int), srcptr, length);
            }
        }
        #endregion

        public int Length => position;

        public int Capacity => buffer?.Length ?? 0;

        public Allocator(byte[] arrayPool)
        {
            if (arrayPool == null)
                ThrowHelper.ThrowArgumentNull();
            buffer = arrayPool;
            position = 0;
        }

        public ReadOnlyMemory<byte> AsMemory()
        {
            return new ReadOnlyMemory<byte>(buffer, 0, position);
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return new ReadOnlySpan<byte>(buffer, 0, position);
        }

        public byte[] ToArray()
        {
            return AsSpan().ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Allocate(int length)
        {
            var offset = position;
            var target = buffer;
            if (target == null || (uint)length > (uint)(target.Length - offset))
                target = ReAllocate(offset, length);
            position = offset + length;
            return new Span<byte>(target, offset, length);
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
                if (target == null || (uint)byteCount > (uint)(target.Length - offset))
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
            fixed (byte* dstptr = Allocate(limits))
            fixed (byte* srcptr = span)
                Unsafe.Copy(dstptr, srcptr, limits);
        }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Allocator)}(Length: {Length}, Capacity: {Capacity})";
        #endregion
    }
}
