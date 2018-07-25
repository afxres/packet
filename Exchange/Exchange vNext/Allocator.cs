using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public readonly struct Allocator
    {
        internal static FieldInfo FieldInfo { get; } = typeof(Allocator).GetField(nameof(stream), BindingFlags.Instance | BindingFlags.NonPublic);

        internal readonly UnsafeStream stream;

        internal Allocator(UnsafeStream stream) => this.stream = stream;

        public Memory<byte> Allocate(int length)
        {
            var offset = stream.Allocate(length, out var target);
            return new Memory<byte>(target, offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(Span<byte> span)
        {
            if (span.IsEmpty)
                return;
            var offset = stream.Allocate(span.Length, out var target);
            Unsafe.CopyBlockUnaligned(ref target[offset], ref span[0], (uint)span.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(ReadOnlySpan<char> span) => stream.Append(span);

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
