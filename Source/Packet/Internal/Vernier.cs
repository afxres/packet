using Mikodev.Network.Converters;

namespace Mikodev.Network.Internal
{
    internal struct Vernier
    {
        internal byte[] Buffer { get; }

        internal int Limits { get; }

        internal int Offset { get; private set; }

        internal int Length { get; private set; }

        internal bool Any => this.Limits - this.Offset != this.Length;

        internal Vernier(Block block)
        {
            this.Buffer = block.Buffer;
            this.Offset = block.Offset;
            this.Limits = block.Limits;
            this.Length = 0;
        }

        internal void Flush()
        {
            this.Offset += this.Length;
            if ((uint)(this.Limits - this.Offset) < sizeof(int))
                goto fail;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref this.Buffer[this.Offset]);
            this.Offset += sizeof(int);
            if ((uint)(this.Limits - this.Offset) < (uint)length)
                goto fail;
            this.Length = length;
            return;

        fail:
            throw PacketException.Overflow();
        }

        internal void FlushExcept(int define)
        {
            if (define > 0)
            {
                this.Offset += this.Length;
                if ((uint)(this.Limits - this.Offset) < (uint)define)
                    throw PacketException.Overflow();
                this.Length = define;
            }
            else
            {
                this.Flush();
            }
        }

        internal bool TryFlush()
        {
            this.Offset += this.Length;
            if ((uint)(this.Limits - this.Offset) < sizeof(int))
                return false;
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref this.Buffer[this.Offset]);
            this.Offset += sizeof(int);
            if ((uint)(this.Limits - this.Offset) < (uint)length)
                return false;
            this.Length = length;
            return true;
        }

        public static explicit operator Block(Vernier vernier) => new Block(vernier);

        public static explicit operator Vernier(Block block) => new Vernier(block);
    }
}
