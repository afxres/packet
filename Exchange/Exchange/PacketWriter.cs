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

        internal readonly ConverterDictionary _converters = null;
        internal object _item = null;

        internal PacketWriter(ConverterDictionary converters, object item)
        {
            _converters = converters;
            _item = item;
        }

        public PacketWriter(ConverterDictionary converters = null) => _converters = converters;

        internal PacketWriterDictionary _GetItems()
        {
            if (_item is PacketWriterDictionary dic)
                return dic;
            var val = new PacketWriterDictionary();
            _item = val;
            return val;
        }

        public byte[] GetBytes()
        {
            var obj = _item;
            if (obj == null)
                return _Extension.s_empty_bytes;
            else if (obj is byte[] buf)
                return buf;
            else if (obj is MemoryStream raw)
                return raw.ToArray();
            var mst = new MemoryStream(_Length);
            _GetBytes(mst, obj, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var obj = _item;
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

        internal static void _GetBytes(Stream stream, object item, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            if (item is PacketWriterDictionary dictionary)
            {
                foreach (var i in dictionary)
                {
                    stream._WriteKey(i.Key);
                    _GetBytes(stream, i.Value, level);
                }
            }
            else if (item is List<PacketWriter> list)
            {
                foreach (var i in list)
                {
                    //stream._BeginInternal(out var sourceEx);
                    _GetBytes(stream, i, level);
                    //stream._EndInternal(sourceEx);
                }
            }
            else if (item is Entry entry)
            {
                var length = entry.Length;
                var pairs = entry.KeyValuePairs;
                foreach (var i in pairs)
                {
                    if (length > 0)
                        stream.Write(i.Key, 0, length);
                    else
                        stream._WriteExt(i.Key);
                    //stream._BeginInternal(out var sourceEx);
                    _GetBytes(stream, i.Value, level);
                    //stream._EndInternal(sourceEx);
                }
            }
            else throw new ApplicationException();
        }

        internal static void _GetBytes(Stream stream, PacketWriter writer, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            var item = writer._item;
            if (item == null)
            {
                stream.Write(_Extension.s_zero_bytes, 0, sizeof(int));
                return;
            }
            if (item is byte[] bytes)
            {
                stream._WriteExt(bytes);
                return;
            }
            if (item is MemoryStream memory)
            {
                stream._WriteExt(memory);
                return;
            }

            stream._BeginInternal(out var source);
            _GetBytes(stream, item, level);
            stream._EndInternal(source);
        }

        internal static PacketWriter _GetWriterEx(ConverterDictionary converters, object item, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            var convert = default(IPacketConverter);
            if (item == null)
                return new PacketWriter(converters);
            if (item is PacketWriter writer)
                return new PacketWriter(converters, writer._item);
            if (item is PacketRawWriter raw)
                return new PacketWriter(converters, raw._stream);

            var type = item.GetType();
            if ((convert = _Caches.GetConverter(converters, type, true)) != null)
                return new PacketWriter(converters, convert._GetBytesWrapError(item));

            var info = _Caches.GetInfo(type);
            var tag = info.Flags;
            if ((tag & _Inf.EnumerableImpl) != 0)
            {
                if (info.ElementType == typeof(byte) && item is ICollection<byte> bytes)
                    return new PacketWriter(converters, bytes._ToBytes());
                if (info.ElementType == typeof(sbyte) && item is ICollection<sbyte> sbytes)
                    return new PacketWriter(converters, sbytes._ToBytes());

                if ((convert = _Caches.GetConverter(converters, info.ElementType, true)) != null)
                    return new PacketWriter(converters, info.FromEnumerable(convert, item));

                var list = new List<PacketWriter>();
                foreach (var i in (item as IEnumerable))
                    list.Add(_GetWriterEx(converters, i, level));
                return new PacketWriter(converters, list);
            }
            else if ((tag & _Inf.EnumerableKeyValuePair) != 0)
            {
                var key = _Caches.GetConverter(converters, info.IndexType, true);
                if (key == null)
                    throw new PacketException(PacketError.InvalidKeyType);
                if ((convert = _Caches.GetConverter(converters, info.ElementType, true)) != null)
                {
                    var value = info.FromEnumerableKeyValuePair(key, convert, item);
                    var result = new PacketWriter(converters, value);
                    return result;
                }
                else if (item is IDictionary<string, object> dictionary)
                {
                    var target = new PacketWriter(converters);
                    var items = target._GetItems();
                    foreach (var i in dictionary)
                        items[i.Key] = _GetWriterEx(converters, i.Value, level);
                    return target;
                }
                else
                {
                    var list = new List<KeyValuePair<byte[], PacketWriter>>();
                    var adapter = info.GetAdapter(key, item);
                    foreach (var i in adapter)
                    {
                        var value = _GetWriterEx(converters, i.Value, level);
                        var pair = new KeyValuePair<byte[], PacketWriter>(i.Key, value);
                        list.Add(pair);
                    }
                    var entry = new Entry { KeyValuePairs = list, Length = key.Length };
                    var result = new PacketWriter(converters, entry);
                    return result;
                }
            }
            else
            {
                var result = new PacketWriter(converters);
                var items = result._GetItems();
                var getter = _Caches.GetGetterInfo(type);
                var values = getter.GetValues(item);
                var arguments = getter.Arguments;
                for (int i = 0; i < arguments.Length; i++)
                    items[arguments[i].Name] = _GetWriterEx(converters, values[i], level);
                return result;
            }
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => _GetWriterEx(converters, value, 0);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
