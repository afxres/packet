using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static Mikodev.Network._Extension;
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

            if (itm == null)
                return Item.Empty;
            if (itm is PacketWriter oth)
                return oth._itm;
            if (itm is PacketRawWriter raw)
                return new Item(raw._str);

            var typ = itm.GetType();
            var inf = default(_Inf);
            if (_Caches.TryGetConverter(cvt, typ, out var con) || ((inf = _Caches.GetInfo(typ)).Flag == _Inf.Enum && s_converters.TryGetValue(inf.ElementType, out con)))
                return new Item(con.GetBytesWrap(itm));

            return GetItemMatch(cvt, itm, lev, inf);
        }

        private static Item GetItemMatch(ConverterDictionary cvt, object itm, int lev, _Inf inf)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            switch (inf.From)
            {
                case _Inf.Bytes:
                    {
                        return new Item(((ICollection<byte>)itm).ToBytes());
                    }
                case _Inf.SBytes:
                    {
                        return new Item(((ICollection<sbyte>)itm).ToBytes());
                    }
                case _Inf.Enumerable:
                    {
                        var ele = inf.ElementType;
                        var sub = default(_Inf);
                        if (_Caches.TryGetConverter(cvt, ele, out var con) || ((sub = _Caches.GetInfo(ele)).Flag == _Inf.Enum && s_converters.TryGetValue(ele, out con)))
                            return new Item(inf.FromEnumerable(con, itm), con.Length);

                        var lst = new List<Item>();
                        foreach (var i in ((IEnumerable)itm))
                            lst.Add(GetItemMatch(cvt, i, lev, sub));
                        return new Item(lst);
                    }
                case _Inf.Dictionary:
                    {
                        var key = _Caches.GetConverter(cvt, inf.IndexType, true);
                        if (key == null)
                            throw PacketException.InvalidKeyType(inf.IndexType);
                        var ele = inf.ElementType;
                        var sub = default(_Inf);
                        if (_Caches.TryGetConverter(cvt, ele, out var con) || ((sub = _Caches.GetInfo(ele)).Flag == _Inf.Enum && s_converters.TryGetValue(ele, out con)))
                            return new Item(inf.FromDictionary(key, con, itm), key.Length, con.Length);

                        var lst = new List<KeyValuePair<byte[], Item>>();
                        var kvp = inf.FromDictionaryAdapter(key, itm);
                        foreach (var i in kvp)
                        {
                            var res = GetItemMatch(cvt, i.Value, lev, sub);
                            var tmp = new KeyValuePair<byte[], Item>(i.Key, res);
                            lst.Add(tmp);
                        }
                        return new Item(lst, key.Length);
                    }
                case _Inf.Map:
                    {
                        var dic = (IDictionary<string, object>)itm;
                        var lst = new Dictionary<string, PacketWriter>();
                        foreach (var i in dic)
                            lst[i.Key] = GetWriter(cvt, i.Value, lev);
                        return new Item(lst);
                    }
                default:
                    {
                        var lst = new Dictionary<string, PacketWriter>();
                        var get = _Caches.GetGetterInfo(inf.Type);
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
