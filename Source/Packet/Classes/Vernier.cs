using Mikodev.Network.Converters;

namespace Mikodev.Network
{
    internal struct Vernier
    {
        internal byte[] Buffer { get; }

        internal int Limits { get; }

        internal int Offset { get; private set; }

        internal int Length { get; private set; }

        internal bool Any => Limits - Offset != Length;

        internal Vernier(Block block)
        {
            Buffer = block.Buffer;
            Offset = block.Offset;
            Limits = block.Limits;
            Length = 0;
        }

        internal void Flush()
        {
            Offset += Length;
            if ((uint)(Limits - Offset) < sizeof(int))
                goto fail;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref Buffer[Offset]);
            Offset += sizeof(int);
            if ((uint)(Limits - Offset) < (uint)length)
                goto fail;
            Length = length;
            return;

            fail:
            throw PacketException.Overflow();
        }

        internal void FlushExcept(int define)
        {
            if (define > 0)
            {
                Offset += Length;
                if ((uint)(Limits - Offset) < (uint)define)
                    throw PacketException.Overflow();
                Length = define;
            }
            else
            {
                Flush();
            }
        }

        internal bool TryFlush()
        {
            Offset += Length;
            if ((uint)(Limits - Offset) < sizeof(int))
                return false;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref Buffer[Offset]);
            Offset += sizeof(int);
            if ((uint)(Limits - Offset) < (uint)length)
                return false;
            Length = length;
            return true;
        }

        public static explicit operator Block(Vernier vernier) => new Block(vernier);

        public static explicit operator Vernier(Block block) => new Vernier(block);
    }
}
