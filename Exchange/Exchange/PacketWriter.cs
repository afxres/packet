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
            var itm = item;
            if (itm.tag == Item.DictionaryPacketWriter)
                return ((Dictionary<string, PacketWriter>)itm.obj).Keys;
            return System.Linq.Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var itm = item;
            if (itm.tag == Item.DictionaryPacketWriter)
                return (Dictionary<string, PacketWriter>)itm.obj;
            var dic = new Dictionary<string, PacketWriter>();
            item = new Item(dic);
            return dic;
        }

        internal static PacketWriter GetWriter(ConverterDictionary converters, object value, int level)
        {
            return new PacketWriter(converters, GetItem(converters, value, level));
        }
        
        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicWriter(parameter, this);

        public byte[] GetBytes()
        {
            var itm = item;
            if (itm.obj == null)
                return UnmanagedArrayConverter<byte>.EmptyArray;
            else if (itm.tag == Item.Bytes)
                return (byte[])itm.obj;
            else if (itm.tag == Item.MemoryStream)
                return ((MemoryStream)itm.obj).ToArray();

            var mst = new MemoryStream(Cache.Length);
            itm.GetBytesMatch(mst, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var obj = item.obj;
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (obj == null)
                stb.Append("none");
            else if (obj is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else if (obj is MemoryStream mst)
                stb.AppendFormat("{0} byte(s)", mst.Length);
            else if (obj is ICollection col)
                stb.AppendFormat("{0} node(s)", col.Count);
            else
                throw new ApplicationException();
            return stb.ToString();
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);
    }
}
