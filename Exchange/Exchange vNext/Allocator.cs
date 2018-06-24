using System;
using System.Reflection;

namespace Mikodev.Binary
{
    public readonly struct Allocator
    {
        internal static FieldInfo FieldInfo { get; } = typeof(Allocator).GetField(nameof(stream), BindingFlags.Instance | BindingFlags.NonPublic);

        internal readonly UnsafeStream stream;

        internal Allocator(UnsafeStream stream) => this.stream = stream;

        public Block Allocate(int length)
        {
            if (stream == null)
                throw new InvalidOperationException();
            var offset = stream.VerifyAvailable(length);
            return new Block(stream.stream, offset, length);
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
