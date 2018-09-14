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
            converters = source.converters;
            block = source.block;
            vernier = (Vernier)block;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            this.converters = converters;
            block = new Block(buffer);
            vernier = (Vernier)block;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            this.converters = converters;
            block = new Block(buffer, offset, length);
            vernier = (Vernier)block;
        }

        internal object Next(PacketConverter converter)
        {
            vernier.FlushExcept(converter.Length);
            return converter.GetObjectChecked(vernier.Buffer, vernier.Offset, vernier.Length);
        }

        internal T Next<T>(PacketConverter<T> converter)
        {
            vernier.FlushExcept(converter.Length);
            return converter.GetValueChecked(vernier.Buffer, vernier.Offset, vernier.Length);
        }


        public bool Any => vernier.Any;

        public void Reset() => vernier = (Vernier)block;

        public override string ToString() => $"{nameof(PacketRawReader)}(Bytes: {block.Length})";
    }
}
