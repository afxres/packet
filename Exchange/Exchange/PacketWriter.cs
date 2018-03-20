using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using PacketWriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        private sealed class Entry
        {
            internal readonly List<KeyValuePair<byte[], PacketWriter>> List;
            internal readonly int Length;

            internal Entry(List<KeyValuePair<byte[], PacketWriter>> list, int length)
            {
                List = list;
                Length = length;
            }
        }

        internal const int _Length = 256;

        internal readonly ConverterDictionary _cvt = null;
        internal object _itm = null;

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
            var mst = new MemoryStream(_Length);
            GetBytes(mst, obj, 0);
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
            else if (obj is Entry ent)
                stb.AppendFormat("{0} key-value pair(s)", ent.List.Count);
            else
                throw new ApplicationException();
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void GetBytes(Stream str, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            if (itm is PacketWriterDictionary dictionary)
            {
                foreach (var i in dictionary)
                {
                    str.WriteKey(i.Key);
                    GetBytes(str, i.Value, lev);
                }
            }
            else if (itm is List<PacketWriter> list)
            {
                foreach (var i in list)
                {
                    GetBytes(str, i, lev);
                }
            }
            else if (itm is Entry entry)
            {
                var len = entry.Length;
                var kvp = entry.List;
                foreach (var i in kvp)
                {
                    if (len > 0)
                        str.Write(i.Key, 0, len);
                    else
                        str.WriteExt(i.Key);
                    GetBytes(str, i.Value, lev);
                }
            }
            else throw new ApplicationException();
        }

        internal static void GetBytes(Stream str, PacketWriter wtr, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var itm = wtr._itm;
            if (itm == null)
            {
                str.Write(_Extension.s_zero_bytes, 0, sizeof(int));
                return;
            }
            if (itm is byte[] bytes)
            {
                str.WriteExt(bytes);
                return;
            }
            if (itm is MemoryStream memory)
            {
                str.WriteExt(memory);
                return;
            }

            str.BeginInternal(out var src);
            GetBytes(str, itm, lev);
            str.FinshInternal(src);
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
                    var lst = new List<KeyValuePair<byte[], PacketWriter>>();
                    var kvp = inf.GetEnumerableKeyValuePairAdapter(key, itm);
                    foreach (var i in kvp)
                    {
                        var val = GetWriter(cvt, i.Value, lev);
                        var tmp = new KeyValuePair<byte[], PacketWriter>(i.Key, val);
                        lst.Add(tmp);
                    }
                    var ent = new Entry(lst, key.Length);
                    var res = new PacketWriter(cvt, ent);
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
