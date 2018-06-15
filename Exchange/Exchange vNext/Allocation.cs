using System;
using System.ComponentModel;

namespace Mikodev.Binary
{
    public readonly struct Allocation
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int length;

        internal Allocation(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                if (length != 0)
                    throw new ArgumentOutOfRangeException();
                this = default;
            }
            else
            {
                if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                    throw new ArgumentOutOfRangeException();
                this.buffer = buffer;
                this.offset = offset;
                this.length = length;
            }
        }

        public bool IsEmpty => length == 0;

        public byte[] Buffer => buffer;

        public int Offset => offset;

        public int Length => length;

        public ref byte Location
        {
            get
            {
                if (length == 0)
                    throw new InvalidOperationException();
                return ref buffer[offset];
            }
        }

        #region override
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Allocation)} with {length} byte(s)";
        #endregion
    }
}
