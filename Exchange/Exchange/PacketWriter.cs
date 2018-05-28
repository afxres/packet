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

        private static Item GetItem(ConverterDictionary converters, object value, int level)
        {
            PacketException.VerifyRecursionError(ref level);

            if (value == null)
                return Item.Empty;

            var typ = value.GetType();
            var inf = Cache.GetConverterOrInfo(converters, typ, out var con);
            if (inf == null)
                return new Item(con.GetBytesWrap(value));

            return GetItemMatch(converters, value, level, inf);
        }

        private static Item GetItemMatch(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);

            switch (valueInfo.From)
            {
                case InfoFlags.Writer:
                    return ((PacketWriter)value).item;
                case InfoFlags.RawWriter:
                    return new Item(((PacketRawWriter)value).stream);
                case InfoFlags.Bytes:
                    return new Item(((ICollection<byte>)value).ToBytes());
                case InfoFlags.SBytes:
                    return new Item(((ICollection<sbyte>)value).ToBytes());

                case InfoFlags.Enumerable:
                    {
                        var ele = valueInfo.ElementType;
                        var inf = Cache.GetConverterOrInfo(converters, ele, out var con);
                        if (inf == null)
                            return new Item(valueInfo.FromEnumerable(con, value), con.Length);

                        var lst = new List<Item>();
                        foreach (var i in ((IEnumerable)value))
                            lst.Add(GetItemMatch(converters, i, level, inf));
                        return new Item(lst);
                    }
                case InfoFlags.Dictionary:
                    {
                        var key = Cache.GetConverter(converters, valueInfo.IndexType, true);
                        if (key == null)
                            throw PacketException.InvalidKeyType(valueInfo.IndexType);
                        var ele = valueInfo.ElementType;
                        var inf = Cache.GetConverterOrInfo(converters, ele, out var con);
                        if (inf == null)
                            return new Item(valueInfo.FromDictionary(key, con, value), key.Length, con.Length);

                        var lst = new List<KeyValuePair<byte[], Item>>();
                        var kvp = valueInfo.FromDictionaryAdapter(key, value);
                        foreach (var i in kvp)
                        {
                            var res = GetItemMatch(converters, i.Value, level, inf);
                            var tmp = new KeyValuePair<byte[], Item>(i.Key, res);
                            lst.Add(tmp);
                        }
                        return new Item(lst, key.Length);
                    }
                case InfoFlags.Expando:
                    {
                        var dic = (IDictionary<string, object>)value;
                        var lst = new Dictionary<string, PacketWriter>();
                        foreach (var i in dic)
                            lst[i.Key] = GetWriter(converters, i.Value, level);
                        return new Item(lst);
                    }
                default:
                    {
                        var lst = new Dictionary<string, PacketWriter>();
                        var get = Cache.GetGetInfo(valueInfo.Type);
                        var val = get.GetValues(value);
                        var arg = get.Arguments;
                        for (int i = 0; i < arg.Length; i++)
                            lst[arg[i].Key] = GetWriter(converters, val[i], level);
                        return new Item(lst);
                    }
            }
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

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
