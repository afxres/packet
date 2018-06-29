using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public readonly struct Block
    {
        #region fields
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int length;
        #endregion

        public bool IsEmpty => length == 0;

        public byte[] Buffer => buffer;

        public int Offset => offset;

        public int Length => length;

        public ref byte Location
        {
            get
            {
                if (length == 0)
                    ThrowHelper.ThrowEmptyBlock();
                return ref buffer[offset];
            }
        }

        internal Block(Vernier vernier)
        {
            buffer = vernier.Buffer;
            offset = vernier.Offset;
            length = vernier.Length;
        }

        public Block(byte[] buffer)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            offset = 0;
            length = buffer.Length;
        }

        public Block(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException();
            this.buffer = buffer;
            this.offset = offset;
            this.length = length;
        }

        public byte[] ToArray()
        {
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[offset], (uint)length);
            return target;
        }

        public static implicit operator ArraySegment<byte>(Block block) => new ArraySegment<byte>(block.buffer, block.offset, block.length);

        public static explicit operator Block(ArraySegment<byte> segment) => new Block(segment.Array, segment.Offset, segment.Count);

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Block)} byte length : {length}";
        #endregion
    }
}
