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
        internal readonly ConverterDictionary _cvt;
        private Item _itm;

        internal PacketWriter(ConverterDictionary cvt, Item itm)
        {
            _itm = itm;
            _cvt = cvt;
        }

        internal PacketWriter(ConverterDictionary cvt, PacketWriter wtr)
        {
            _cvt = cvt;
            _itm = (wtr != null ? wtr._itm : Item.Empty);
        }

        public PacketWriter(ConverterDictionary converters = null)
        {
            _cvt = converters;
            _itm = Item.Empty;
        }

        internal IEnumerable<string> GetKeys()
        {
            var itm = _itm;
            if (itm.tag == Item.DictionaryPacketWriter)
                return ((Dictionary<string, PacketWriter>)itm.obj).Keys;
            return Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var itm = _itm;
            if (itm.tag == Item.DictionaryPacketWriter)
                return (Dictionary<string, PacketWriter>)itm.obj;
            var val = new Dictionary<string, PacketWriter>();
            _itm = new Item(val);
            return val;
        }

        internal static PacketWriter GetWriter(ConverterDictionary cvt, object itm, int lev)
        {
            return new PacketWriter(cvt, GetItem(cvt, itm, lev));
        }

        private static Item GetItem(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var con = default(IPacketConverter);
            if (itm == null)
                return Item.Empty;
            if (itm is PacketWriter oth)
                return oth._itm;
            if (itm is PacketRawWriter raw)
                return new Item(raw._str);

            var typ = itm.GetType();
            if ((con = _Caches.GetConverter(cvt, typ, true)) != null)
                return new Item(con.GetBytesWrap(itm));

            var inf = _Caches.GetInfo(typ);
            var tag = inf.From;
            switch (tag)
            {
                case _Inf.Enumerable:
                    {
                        if (inf.ElementType == typeof(byte) && itm is ICollection<byte> bytes)
                            return new Item(bytes.ToBytes());
                        if (inf.ElementType == typeof(sbyte) && itm is ICollection<sbyte> sbytes)
                            return new Item(sbytes.ToBytes());

                        if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                            return new Item(inf.FromEnumerable(con, itm), con.Length);

                        var lst = new List<Item>();
                        foreach (var i in (itm as IEnumerable))
                            lst.Add(GetItem(cvt, i, lev));
                        return new Item(lst);
                    }
                case _Inf.Mapping:
                    {
                        var dic = (IDictionary<string, object>)itm;
                        var lst = new Dictionary<string, PacketWriter>();
                        foreach (var i in dic)
                            lst[i.Key] = GetWriter(cvt, i.Value, lev);
                        return new Item(lst);
                    }
                case _Inf.Dictionary:
                    {
                        var key = _Caches.GetConverter(cvt, inf.IndexerType, true);
                        if (key == null)
                            throw PacketException.InvalidKeyType(inf.IndexerType);
                        if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                        {
                            var val = inf.FromDictionary(key, con, itm);
                            return new Item(val, key.Length, con.Length);
                        }
                        else
                        {
                            var val = new List<KeyValuePair<byte[], Item>>();
                            var kvp = inf.FromDictionaryAdapter(key, itm);
                            foreach (var i in kvp)
                            {
                                var sub = GetItem(cvt, i.Value, lev);
                                var tmp = new KeyValuePair<byte[], Item>(i.Key, sub);
                                val.Add(tmp);
                            }
                            return new Item(val, key.Length);
                        }
                    }
                default:
                    {
                        var lst = new Dictionary<string, PacketWriter>();
                        var get = _Caches.GetGetterInfo(typ);
                        var val = get.GetValues(itm);
                        var arg = get.Arguments;
                        for (int i = 0; i < arg.Length; i++)
                            lst[arg[i].Name] = GetWriter(cvt, val[i], lev);
                        return new Item(lst);
                    }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        public byte[] GetBytes()
        {
            var itm = _itm;
            if (itm.obj == null)
                return _Extension.s_empty_bytes;
            else if (itm.tag == Item.Bytes)
                return (byte[])itm.obj;
            else if (itm.tag == Item.MemoryStream)
                return ((MemoryStream)itm.obj).ToArray();

            var mst = new MemoryStream(_Caches.Length);
            itm.GetBytesExtra(mst, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var obj = _itm.obj;
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
