using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using PacketReaderDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketReader>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        internal const int _Init = 1;
        internal const int _InitList = 2;

        internal int _flags = 0;
        internal _Element _element;
        internal List<PacketReader> _list = null;
        internal PacketReaderDictionary _dictionary = null;
        internal readonly ConverterDictionary _converters;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer);
            _converters = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer, offset, length);
            _converters = converters;
        }

        internal PacketReaderDictionary _GetItemDictionary()
        {
            var obj = _dictionary;
            if (obj != null)
                return obj;
            if ((_flags & _Init) != 0)
                return null;
            _flags |= _Init;

            var dic = new PacketReaderDictionary();
            var buf = _element._buffer;
            var max = _element.Max();
            var idx = _element._offset;
            var len = 0;

            while (idx < max)
            {
                if (buf._HasNext(max, ref idx, out len) == false)
                    return null;
                var key = Encoding.UTF8.GetString(buf, idx, len);
                if (dic.ContainsKey(key))
                    return null;
                idx += len;
                if (buf._HasNext(max, ref idx, out len) == false)
                    return null;
                dic.Add(key, new PacketReader(buf, idx, len, _converters));
                idx += len;
            }

            _dictionary = dic;
            return dic;
        }

        internal List<PacketReader> _GetItemList()
        {
            var lst = _list;
            if (lst != null)
                return lst;
            if ((_flags & _InitList) != 0)
                throw PacketException.ThrowOverflow();
            _flags |= _InitList;

            lst = new List<PacketReader>();
            var max = _element.Max();
            var idx = _element._offset;
            var buf = _element._buffer;
            while (idx != max)
            {
                if (buf._HasNext(max, ref idx, out var length) == false)
                    throw PacketException.ThrowOverflow();
                var rea = new PacketReader(buf, idx, length, _converters);
                lst.Add(rea);
                idx += length;
            }
            _list = lst;
            return lst;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader _GetItem(string key, bool nothrow)
        {
            var dic = _GetItemDictionary();
            if (dic != null && dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(dic == null ? PacketError.Overflow : PacketError.PathError);
        }

        /// <summary>
        /// <paramref name="keys"/> not null
        /// </summary>
        internal PacketReader _GetItem(IEnumerable<string> keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr._GetItem(i ?? throw new ArgumentNullException(), nothrow)) == null)
                    return null;
            return rdr;
        }

        public int Count => _GetItemDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => _GetItemDictionary()?.Keys ?? Enumerable.Empty<string>();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(_Extension.s_separators);
                var val = _GetItem(key, nothrow);
                return val;
            }
        }

        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            var dic = _GetItemDictionary();
            if (dic != null)
                stb.AppendFormat("{0} node(s), ", dic.Count);
            stb.AppendFormat("{0} byte(s)", _element._length);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        internal object _GetValue(Type type, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            if (type == typeof(object) || type == typeof(PacketReader))
                return this;
            if (type == typeof(PacketRawReader))
                return new PacketRawReader(this);

            var convert = _Caches.GetConverter(_converters, type, true);
            if (convert != null)
                return convert._GetValueWrapError(_element, true);

            var info = _Caches.GetInfo(type);
            var tag = info.Flags;
            var element = info.ElementType;
            if (element != null)
                convert = _Caches.GetConverter(_converters, element, true);

            if ((tag & _Inf.Array) != 0)
            {
                if (convert != null)
                    return info.GetArray(_element, convert);
                var values = _GetValueArray(element, level);
                var result = info.CastToArray(values);
                return result;
            }
            if ((tag & _Inf.List) != 0)
            {
                if (convert != null)
                    return info.GetList(_element, convert);
                var values = _GetValueArray(element, level);
                var result = info.CastToList(values);
                return result;
            }
            if ((tag & _Inf.Enumerable) != 0)
            {
                if (convert != null)
                    return info.GetEnumerable(_element, convert);
                return info.GetEnumerableReader(this, level);
            }
            else if ((tag & _Inf.Collection) != 0)
            {
                if (convert != null)
                    return info.GetCollection(_element, convert);
                var values = _GetValueArray(element, level);
                var result = info.CastToCollection(values);
                return result;
            }
            else if ((tag & _Inf.Dictionary) != 0)
            {
                var keycon = _Caches.GetConverter(_converters, info.IndexType, true);
                if (keycon == null)
                    throw new PacketException(PacketError.InvalidKeyType);
                if (convert != null)
                    return info.GetDictionary(_element, keycon, convert);

                var max = _element.Max();
                var idx = _element._offset;
                var buf = _element._buffer;
                var keylen = keycon.Length;
                var len = 0;

                var lst = new List<KeyValuePair<object, object>>();
                while (true)
                {
                    var sub = max - idx;
                    if (sub == 0)
                        break;
                    if (keylen > 0)
                        if (sub < keylen)
                            throw PacketException.ThrowOverflow();
                        else
                            len = keylen;
                    else if (buf._HasNext(max, ref idx, out len) == false)
                        throw PacketException.ThrowOverflow();
                    // Wrap error non-check
                    var key = keycon._GetValueWrapError(buf, idx, len, false);
                    idx += len;

                    if (buf._HasNext(max, ref idx, out len) == false)
                        throw PacketException.ThrowOverflow();
                    var rea = new PacketReader(buf, idx, len, _converters);
                    var val = rea._GetValue(element, level);
                    var par = new KeyValuePair<object, object>(key, val);

                    idx += len;
                    lst.Add(par);
                }
                return info.CastToDictionary(lst);
            }
            else
            {
                var setter = _Caches.GetSetterInfo(type);
                var arguments = setter.Arguments;
                var function = setter.Function;
                if (arguments == null || function == null)
                    throw new PacketException(PacketError.InvalidType);

                var values = new object[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    var reader = _GetItem(arguments[i].Name, false);
                    var value = reader._GetValue(arguments[i].Type, level);
                    values[i] = value;
                }

                var result = function.Invoke(values);
                return result;
            }
        }

        internal object[] _GetValueArray(Type element, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            var lst = _GetItemList();
            var arr = new object[lst.Count];
            for (int i = 0; i < lst.Count; i++)
                arr[i] = lst[i]._GetValue(element, level);
            return arr;
        }

        public T Deserialize<T>(T anonymous) => (T)_GetValue(typeof(T), 0);

        public T Deserialize<T>() => (T)_GetValue(typeof(T), 0);

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return _GetValue(type, 0);
        }
    }
}
