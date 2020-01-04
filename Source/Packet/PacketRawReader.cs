using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal readonly ConverterDictionary converters;

        private readonly Block block;

        private Vernier vernier;

        public PacketRawReader(PacketReader source)
        {
            this.converters = source.converters;
            this.block = source.block;
            this.vernier = (Vernier)this.block;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            this.converters = converters;
            this.block = new Block(buffer);
            this.vernier = (Vernier)this.block;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            this.converters = converters;
            this.block = new Block(buffer, offset, length);
            this.vernier = (Vernier)this.block;
        }

        internal object Next(PacketConverter converter)
        {
            this.vernier.FlushExcept(converter.Length);
            return converter.GetObjectChecked(this.vernier.Buffer, this.vernier.Offset, this.vernier.Length);
        }

        internal T Next<T>(PacketConverter<T> converter)
        {
            this.vernier.FlushExcept(converter.Length);
            return converter.GetValueChecked(this.vernier.Buffer, this.vernier.Offset, this.vernier.Length);
        }

        public bool Any => this.vernier.Any;

        public void Reset() => this.vernier = (Vernier)this.block;

        public override string ToString() => $"{nameof(PacketRawReader)}(Bytes: {this.block.Length})";
    }
}
