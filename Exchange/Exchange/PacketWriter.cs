using Mikodev.Network.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
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
            if (item.flag == ItemFlags.Dictionary)
                return ((Dictionary<string, PacketWriter>)item.value).Keys;
            return System.Linq.Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var item = this.item;
            if (item.flag == ItemFlags.Dictionary)
                return (Dictionary<string, PacketWriter>)item.value;
            var dictionary = new Dictionary<string, PacketWriter>();
            this.item = new Item(dictionary);
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
                    return UnmanagedArrayConverter<byte>.EmptyArray;
                case ItemFlags.Buffer:
                    return (byte[])item.value;
                case ItemFlags.Stream:
                    return ((MemoryStream)item.value).ToArray();
                default:
                    var mst = new MemoryStream(Cache.Length);
                    item.GetBytesMatch(mst, 0);
                    return mst.ToArray();
            }
        }

        public override string ToString()
        {
            var value = item.value;
            var builder = new StringBuilder(nameof(PacketWriter));
            builder.Append(" with ");
            if (value == null)
                builder.Append("none");
            else if (value is byte[] buf)
                builder.AppendFormat("{0} byte(s)", buf.Length);
            else if (value is MemoryStream mst)
                builder.AppendFormat("{0} byte(s)", mst.Length);
            else if (value is ICollection col)
                builder.AppendFormat("{0} node(s)", col.Count);
            else
                throw new ApplicationException();
            return builder.ToString();
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);
    }
}
