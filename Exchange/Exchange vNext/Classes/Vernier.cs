using Mikodev.Binary.Converters;
using System;

namespace Mikodev.Binary
{
    internal struct Vernier
    {
        private readonly byte[] buffer;
        private readonly int limits;
        private int offset;
        private int length;

        public byte[] Buffer => buffer;

        public int Offset => offset;

        public int Length => length;

        public bool Any => limits - offset != length;

        public Vernier(Block block)
        {
            buffer = block.Buffer;
            offset = block.Offset;
            limits = block.Offset + block.Length;
            length = 0;
        }

        public void Flush()
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                goto fail;
            var length = UnmanagedValueConverter<int>.ToValueUnchecked(ref buffer[offset]);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                goto fail;
            this.length = length;
            return;

            fail:
            throw new OverflowException();
        }

        public Block FlushBlock()
        {
            Flush();
            return new Block(buffer, offset, length);
        }
    }
}
