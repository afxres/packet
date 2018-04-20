using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal readonly ConverterDictionary converters;
        private Item item;

        internal PacketWriter(ConverterDictionary cvt, Item itm)
        {
            converters = cvt;
            item = itm;
        }

        internal PacketWriter(ConverterDictionary cvt, PacketWriter wtr)
        {
            converters = cvt;
            item = (wtr != null ? wtr.item : Item.Empty);
        }

        public PacketWriter(ConverterDictionary converters = null)
        {
            this.converters = converters;
            item = Item.Empty;
        }

        internal IEnumerable<string> GetKeys()
        {
            var item = this.item;
            if (item.tag == Item.DictionaryPacketWriter)
                return ((Dictionary<string, PacketWriter>)item.obj).Keys;
            return System.Linq.Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var item = this.item;
            if (item.tag == Item.DictionaryPacketWriter)
                return (Dictionary<string, PacketWriter>)item.obj;
            var dictionary = new Dictionary<string, PacketWriter>();
            this.item = new Item(dictionary);
            return dictionary;
        }

        internal static PacketWriter GetWriter(ConverterDictionary converters, object value, int level)
        {
            return new PacketWriter(converters, GetItem(converters, value, level));
        }

        private static Item GetItem(ConverterDictionary converters, object value, int level)
        {
            if (level > Cache.Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            if (value == null)
                return Item.Empty;

            var typ = value.GetType();
            var inf = default(Info);
            if (Cache.TryGetConverter(converters, typ, out var con, ref inf))
                return new Item(con.GetBytesWrap(value));

            return GetItemMatch(converters, value, level, inf);
        }

        private static Item GetItemMatch(ConverterDictionary converters, object value, int level, Info info)
        {
            if (level > Cache.Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            switch (info.From)
            {
                case Info.Writer:
                    return ((PacketWriter)value).item;
                case Info.RawWriter:
                    return new Item(((PacketRawWriter)value).stream);
                case Info.Bytes:
                    return new Item(((ICollection<byte>)value).ToBytes());
                case Info.SBytes:
                    return new Item(((ICollection<sbyte>)value).ToBytes());

                case Info.Enumerable:
                    {
                        var ele = info.ElementType;
                        var sub = default(Info);
                        if (Cache.TryGetConverter(converters, ele, out var con, ref sub))
                            return new Item(info.FromEnumerable(con, value), con.Length);

                        var lst = new List<Item>();
                        foreach (var i in ((IEnumerable)value))
                            lst.Add(GetItemMatch(converters, i, level, sub));
                        return new Item(lst);
                    }
                case Info.Dictionary:
                    {
                        var key = Cache.GetConverter(converters, info.IndexType, true);
                        if (key == null)
                            throw PacketException.InvalidKeyType(info.IndexType);
                        var ele = info.ElementType;
                        var sub = default(Info);
                        if (Cache.TryGetConverter(converters, ele, out var con, ref sub))
                            return new Item(info.FromDictionary(key, con, value), key.Length, con.Length);

                        var lst = new List<KeyValuePair<byte[], Item>>();
                        var kvp = info.FromDictionaryAdapter(key, value);
                        foreach (var i in kvp)
                        {
                            var res = GetItemMatch(converters, i.Value, level, sub);
                            var tmp = new KeyValuePair<byte[], Item>(i.Key, res);
                            lst.Add(tmp);
                        }
                        return new Item(lst, key.Length);
                    }
                case Info.Map:
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
                        var get = Cache.GetGetterInfo(info.Type);
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
                return Extension.s_empty_bytes;
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
