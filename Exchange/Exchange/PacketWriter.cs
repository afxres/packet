﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using KeyValuePairList = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<byte[], Mikodev.Network.PacketWriter>>;
using PacketWriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int Length = 256;

        internal readonly ConverterDictionary _cvt;
        internal object _itm;
        internal int _keylen;

        internal PacketWriter(ConverterDictionary converters, object item)
        {
            _cvt = converters;
            _itm = item;
        }

        public PacketWriter(ConverterDictionary converters = null) => _cvt = converters;

        internal PacketWriterDictionary GetDictionary()
        {
            if (_itm is PacketWriterDictionary dic)
                return dic;
            var val = new PacketWriterDictionary();
            _itm = val;
            return val;
        }

        public byte[] GetBytes()
        {
            var obj = _itm;
            if (obj == null)
                return _Extension.s_empty_bytes;
            else if (obj is byte[] buf)
                return buf;
            else if (obj is MemoryStream raw)
                return raw.ToArray();
            var mst = new MemoryStream(Length);
            GetBytesExtra(this, mst, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var obj = _itm;
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (obj == null)
                stb.Append("none");
            else if (obj is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else if (obj is MemoryStream mst)
                stb.AppendFormat("{0} byte(s)", mst.Length);
            else if (obj is PacketWriterDictionary dic)
                stb.AppendFormat("{0} node(s)", dic.Count);
            else if (obj is List<PacketWriter> lst)
                stb.AppendFormat("{0} node(s)", lst.Count);
            else if (obj is KeyValuePairList kvp)
                stb.AppendFormat("{0} key-value pair(s)", kvp.Count);
            else
                throw new ApplicationException();
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void GetBytesExtra(PacketWriter wtr, Stream str, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var itm = wtr._itm;
            if (itm is PacketWriterDictionary dic)
            {
                foreach (var i in dic)
                {
                    str.WriteKey(i.Key);
                    GetBytes(i.Value, str, lev);
                }
            }
            else if (itm is List<PacketWriter> lst)
            {
                for (int i = 0; i < lst.Count; i++)
                {
                    GetBytes(lst[i], str, lev);
                }
            }
            else if (itm is KeyValuePairList kvp)
            {
                var len = wtr._keylen;
                for (int i = 0; i < kvp.Count; i++)
                {
                    var cur = kvp[i];
                    if (len > 0)
                        str.Write(cur.Key, 0, len);
                    else
                        str.WriteExt(cur.Key);
                    GetBytes(cur.Value, str, lev);
                }
            }
            else throw new ApplicationException();
        }

        internal static void GetBytes(PacketWriter wtr, Stream str, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var itm = wtr._itm;
            if (itm == null)
            {
                str.Write(_Extension.s_zero_bytes, 0, sizeof(int));
            }
            else if (itm is byte[] buf)
            {
                str.WriteExt(buf);
            }
            else if (itm is MemoryStream mst)
            {
                str.WriteExt(mst);
            }
            else
            {
                str.BeginInternal(out var src);
                GetBytesExtra(wtr, str, lev);
                str.FinshInternal(src);
            }
        }

        internal static PacketWriter GetWriter(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var con = default(IPacketConverter);
            if (itm == null)
                return new PacketWriter(cvt);
            if (itm is PacketWriter writer)
                return new PacketWriter(cvt, writer._itm);
            if (itm is PacketRawWriter raw)
                return new PacketWriter(cvt, raw._str);

            var type = itm.GetType();
            if ((con = _Caches.GetConverter(cvt, type, true)) != null)
                return new PacketWriter(cvt, con.GetBytesWrap(itm));

            var inf = _Caches.GetInfo(type);
            var tag = inf.Flags;
            if ((tag & _Inf.EnumerableImpl) != 0)
            {
                if (inf.ElementType == typeof(byte) && itm is ICollection<byte> bytes)
                    return new PacketWriter(cvt, bytes.ToBytes());
                if (inf.ElementType == typeof(sbyte) && itm is ICollection<sbyte> sbytes)
                    return new PacketWriter(cvt, sbytes.ToBytes());

                if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                    return new PacketWriter(cvt, inf.FromEnumerable(con, itm));

                var lst = new List<PacketWriter>();
                foreach (var i in (itm as IEnumerable))
                    lst.Add(GetWriter(cvt, i, lev));
                return new PacketWriter(cvt, lst);
            }
            else if ((tag & _Inf.DictionaryStringObject) != 0)
            {
                var dic = (IDictionary<string, object>)itm;
                var wtr = new PacketWriter(cvt);
                var lst = wtr.GetDictionary();
                foreach (var i in dic)
                    lst[i.Key] = GetWriter(cvt, i.Value, lev);
                return wtr;
            }
            else if ((tag & _Inf.EnumerableKeyValuePair) != 0)
            {
                var key = _Caches.GetConverter(cvt, inf.IndexType, true);
                if (key == null)
                    throw new PacketException(PacketError.InvalidKeyType);
                if ((con = _Caches.GetConverter(cvt, inf.ElementType, true)) != null)
                {
                    var val = inf.FromEnumerableKeyValuePair(key, con, itm);
                    var res = new PacketWriter(cvt, val);
                    return res;
                }
                else
                {
                    var lst = new KeyValuePairList();
                    var kvp = inf.GetEnumerableKeyValuePairAdapter(key, itm);
                    foreach (var i in kvp)
                    {
                        var val = GetWriter(cvt, i.Value, lev);
                        var tmp = new KeyValuePair<byte[], PacketWriter>(i.Key, val);
                        lst.Add(tmp);
                    }
                    var res = new PacketWriter(cvt, lst) { _keylen = key.Length };
                    return res;
                }
            }
            else
            {
                var res = new PacketWriter(cvt);
                var lst = res.GetDictionary();
                var get = _Caches.GetGetterInfo(type);
                var val = get.GetValues(itm);
                var arg = get.Arguments;
                for (int i = 0; i < arg.Length; i++)
                    lst[arg[i].Name] = GetWriter(cvt, val[i], lev);
                return res;
            }
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
