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
            internal List<KeyValuePair<byte[], PacketWriter>> KeyValuePairs;
            internal int Length;
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
                stb.AppendFormat("{0} key-value pair(s)", ent.KeyValuePairs.Count);
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
                var length = entry.Length;
                var pairs = entry.KeyValuePairs;
                foreach (var i in pairs)
                {
                    if (length > 0)
                        str.Write(i.Key, 0, length);
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

            var item = wtr._itm;
            if (item == null)
            {
                str.Write(_Extension.s_zero_bytes, 0, sizeof(int));
                return;
            }
            if (item is byte[] bytes)
            {
                str.WriteExt(bytes);
                return;
            }
            if (item is MemoryStream memory)
            {
                str.WriteExt(memory);
                return;
            }

            str.BeginInternal(out var src);
            GetBytes(str, item, lev);
            str.FinshInternal(src);
        }

        internal static PacketWriter GetWriter(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var convert = default(IPacketConverter);
            if (itm == null)
                return new PacketWriter(cvt);
            if (itm is PacketWriter writer)
                return new PacketWriter(cvt, writer._itm);
            if (itm is PacketRawWriter raw)
                return new PacketWriter(cvt, raw._str);

            var type = itm.GetType();
            if ((convert = _Caches.GetConverter(cvt, type, true)) != null)
                return new PacketWriter(cvt, convert.GetBytesWrap(itm));

            var info = _Caches.GetInfo(type);
            var tag = info.Flags;
            if ((tag & _Inf.EnumerableImpl) != 0)
            {
                if (info.ElementType == typeof(byte) && itm is ICollection<byte> bytes)
                    return new PacketWriter(cvt, bytes.ToBytes());
                if (info.ElementType == typeof(sbyte) && itm is ICollection<sbyte> sbytes)
                    return new PacketWriter(cvt, sbytes.ToBytes());

                if ((convert = _Caches.GetConverter(cvt, info.ElementType, true)) != null)
                    return new PacketWriter(cvt, info.FromEnumerable(convert, itm));

                var list = new List<PacketWriter>();
                foreach (var i in (itm as IEnumerable))
                    list.Add(GetWriter(cvt, i, lev));
                return new PacketWriter(cvt, list);
            }
            else if ((tag & _Inf.EnumerableKeyValuePair) != 0)
            {
                var key = _Caches.GetConverter(cvt, info.IndexType, true);
                if (key == null)
                    throw new PacketException(PacketError.InvalidKeyType);
                if ((convert = _Caches.GetConverter(cvt, info.ElementType, true)) != null)
                {
                    var value = info.FromEnumerableKeyValuePair(key, convert, itm);
                    var result = new PacketWriter(cvt, value);
                    return result;
                }
                else if (itm is IDictionary<string, object> dictionary)
                {
                    var target = new PacketWriter(cvt);
                    var items = target.GetDictionary();
                    foreach (var i in dictionary)
                        items[i.Key] = GetWriter(cvt, i.Value, lev);
                    return target;
                }
                else
                {
                    var list = new List<KeyValuePair<byte[], PacketWriter>>();
                    var adapter = info.GetEnumerableKeyValuePairAdapter(key, itm);
                    foreach (var i in adapter)
                    {
                        var value = GetWriter(cvt, i.Value, lev);
                        var pair = new KeyValuePair<byte[], PacketWriter>(i.Key, value);
                        list.Add(pair);
                    }
                    var entry = new Entry { KeyValuePairs = list, Length = key.Length };
                    var result = new PacketWriter(cvt, entry);
                    return result;
                }
            }
            else
            {
                var result = new PacketWriter(cvt);
                var items = result.GetDictionary();
                var getter = _Caches.GetGetterInfo(type);
                var values = getter.GetValues(itm);
                var arguments = getter.Arguments;
                for (int i = 0; i < arguments.Length; i++)
                    items[arguments[i].Name] = GetWriter(cvt, values[i], lev);
                return result;
            }
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
