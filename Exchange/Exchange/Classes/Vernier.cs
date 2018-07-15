using Mikodev.Network.Converters;

namespace Mikodev.Network
{
    internal struct Vernier
    {
        private readonly byte[] buffer;
        private readonly int limits;
        private int offset;
        private int length;

        internal byte[] Buffer => buffer;

        internal int Offset => offset;

        internal int Length => length;

        internal bool Any => limits - offset != length;

        internal Vernier(Block block)
        {
            buffer = block.Buffer;
            offset = block.Offset;
            limits = block.Limits;
            length = 0;
        }

        internal void Flush()
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                goto fail;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref buffer[offset]);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                goto fail;
            this.length = length;
            return;

            fail:
            throw PacketException.Overflow();
        }

        internal void FlushExcept(int define)
        {
            if (define > 0)
            {
                offset += length;
                if ((uint)(limits - offset) < (uint)define)
                    throw PacketException.Overflow();
                length = define;
            }
            else
            {
                Flush();
            }
        }

        internal bool TryFlush()
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                return false;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref buffer[offset]);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                return false;
            this.length = length;
            return true;
        }

        public static explicit operator Block(Vernier vernier) => new Block(vernier);

        public static explicit operator Vernier(Block block) => new Vernier(block);
    }
}
