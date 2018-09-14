using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal readonly ConverterDictionary converters;

        private Item item;

        internal PacketWriter(ConverterDictionary converters, Item item)
        {
            this.converters = converters;
            this.item = item;
        }

        internal PacketWriter(ConverterDictionary converters, PacketWriter writer)
        {
            this.converters = converters;
            item = (writer != null ? writer.item : Item.Empty);
        }

        public PacketWriter(ConverterDictionary converters = null)
        {
            this.converters = converters;
            item = Item.Empty;
        }

        internal IEnumerable<string> GetKeys()
        {
            var item = this.item;
            return item.flag == ItemFlags.Dictionary
                ? ((Dictionary<string, PacketWriter>)item.data).Keys
                : System.Linq.Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var item = this.item;
            if (item.flag == ItemFlags.Dictionary)
                return (Dictionary<string, PacketWriter>)item.data;
            var dictionary = new Dictionary<string, PacketWriter>(Extension.Capacity);
            this.item = NewItem(dictionary);
            return dictionary;
        }

        internal static PacketWriter GetWriter(ConverterDictionary converters, object value, int level)
        {
            return new PacketWriter(converters, GetItem(converters, value, level));
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicWriter(parameter, this);

        public byte[] GetBytes()
        {
            var item = this.item;
            switch (item.flag)
            {
                case ItemFlags.None:
                    return Empty.Array<byte>();
                case ItemFlags.Buffer:
                    return (byte[])item.data;
                default:
                    var stream = new UnsafeStream();
                    item.GetBytesMatch(stream, 0);
                    return stream.GetBytes();
            }
        }

        public override string ToString()
        {
            var data = item.data;
            if (data is byte[] bytes)
                return $"{nameof(PacketWriter)}(Bytes: {bytes.Length})";
            return $"{nameof(PacketWriter)}(Nodes: {(data as ICollection)?.Count ?? 0})";
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);
    }
}
