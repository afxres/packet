using System;
using System.ComponentModel;

namespace Mikodev.Binary
{
    public readonly struct Allocator
    {
        internal readonly UnsafeStream stream;

        internal Allocator(UnsafeStream stream) => this.stream = stream;

        public Allocation Allocate(int length)
        {
            if (stream == null)
                throw new InvalidOperationException();
            var offset = stream.VerifyAvailable(length);
            return new Allocation(stream.stream, offset, length);
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
