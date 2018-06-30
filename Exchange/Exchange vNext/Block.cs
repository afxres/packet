using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    [DebuggerTypeProxy(typeof(BlockDebug))]
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

        public ref byte this[int index]
        {
            get
            {
                if ((uint)index > (uint)length)
                    ThrowHelper.ThrowArgumentOutOfRange();
                return ref buffer[offset + index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block(byte[] buffer)
        {
            if (buffer == null)
                ThrowHelper.ThrowArgumentNull();
            this.buffer = buffer;
            offset = 0;
            length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Block(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                ThrowHelper.ThrowArgumentNull();
            if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                ThrowHelper.ThrowArgumentOutOfRange();
            this.buffer = buffer;
            this.offset = offset;
            this.length = length;
        }

        public Block Slice(int start) => new Block(buffer, offset + start, length - start);

        public Block Slice(int start, int count) => new Block(buffer, offset + start, count);

        public byte[] ToArray()
        {
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[offset], (uint)length);
            return target;
        }

        public static implicit operator ArraySegment<byte>(Block block) => new ArraySegment<byte>(block.buffer, block.offset, block.length);

        public static implicit operator Block(ArraySegment<byte> segment) => new Block(segment.Array, segment.Offset, segment.Count);

        public static implicit operator Block(byte[] array) => new Block(array);

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
