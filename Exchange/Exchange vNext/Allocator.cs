using System;
using System.ComponentModel;
using System.Reflection;

namespace Mikodev.Binary
{
    public readonly struct Allocator
    {
        internal static FieldInfo FileInfo { get; } = typeof(Allocator).GetField(nameof(stream), BindingFlags.Instance | BindingFlags.NonPublic);

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => nameof(Allocator);
        #endregion
    }
}
