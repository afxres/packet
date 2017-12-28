using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        internal _Element _spa;
        internal Dictionary<string, PacketReader> _dic = null;
        internal readonly ConverterDictionary _con = null;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer);
            _con = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _con = converters;
        }

        internal bool _Init()
        {
            if (_dic != null)
                return true;
            if (_spa._idx != _spa._off)
                return false;
            var dic = new Dictionary<string, PacketReader>();
            var len = 0;

            while (_spa._idx < _spa._max)
            {
                if (_spa._buf._Read(_spa._max, ref _spa._idx, out len) == false)
                    return false;
                var key = Encoding.UTF8.GetString(_spa._buf, _spa._idx, len);
                if (dic.ContainsKey(key))
                    return false;
                _spa._idx += len;
                if (_spa._buf._Read(_spa._max, ref _spa._idx, out len) == false)
                    return false;
                dic.Add(key, new PacketReader(_spa._buf, _spa._idx, len, _con));
                _spa._idx += len;
            }

            _dic = dic;
            return true;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader _GetItem(string key, bool nothrow)
        {
            var res = _Init();
            if (res == true && _dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(res ? PacketError.PathError : PacketError.Overflow);
        }

        /// <summary>
        /// <paramref name="keys"/> not null
        /// </summary>
        internal PacketReader _GetItem(IEnumerable<string> keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr._GetItem(i, nothrow)) == null)
                    return null;
            return rdr;
        }

        public int Count => _Init() ? _dic.Count : 0;

        public IEnumerable<string> Keys => _Init() ? _dic.Keys : Enumerable.Empty<string>();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(_Extension.s_seps);
                var val = _GetItem(key, nothrow);
                return val;
            }
        }

        [Obsolete]
        public PacketReader this[string path, bool nothrow, char[] split] => _GetItem(path?.Split(split ?? _Extension.s_seps), nothrow);

        [Obsolete]
        public PacketReader Pull(string key, bool nothrow = false) => _GetItem(key, nothrow);

        [Obsolete]
        public PacketReader Pull(string[] keys, bool nothrow = false)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            if (keys.Length < 1)
                throw new ArgumentException("Key collection can not be empty!");
            return _GetItem(keys, nothrow);
        }

        [Obsolete]
        public object Pull(Type type) => _Caches.Converter(type, _con, false)._GetValueWrapError(_spa._buf, _spa._off, _spa._len, true);

        [Obsolete]
        public T Pull<T>() => _Caches.Converter(typeof(T), _con, false)._GetValueWrapErrorAuto<T>(_spa._buf, _spa._off, _spa._len, true);

        [Obsolete]
        public byte[] PullList() => _spa._buf._ToBytes(_spa._off, _spa._len);

        [Obsolete]
        public IEnumerable PullList(Type type) => new _Enumerable(this, type);

        [Obsolete]
        public IEnumerable<T> PullList<T>() => new _Enumerable<T>(this);

        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            if (_Init())
                stb.AppendFormat("{0} node(s), ", _dic.Count);
            stb.AppendFormat("{0} byte(s)", _spa._len);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        internal bool _Convert(Type type, out object value)
        {
            var val = default(object);
            var con = default(IPacketConverter);

            if (type == typeof(PacketReader))
                val = this;
            else if (type == typeof(PacketRawReader))
                val = new PacketRawReader(this);
            else if ((con = _Caches.Converter(type, _con, true)) != null)
                val = con._GetValueWrapError(_spa._buf, _spa._off, _spa._len, true);
            else if (type._IsArray(out var inn))
                val = _Caches.Array(this, inn);
            else if (type._IsEnumerable(out inn))
                val = _Caches.Enumerable(this, inn);
            else if (type._IsList(out inn))
                val = _Caches.List(this, inn);
            else goto fail;

            value = val;
            return true;

            fail:
            value = null;
            return false;
        }

        public T Deserialize<T>(T anonymous) => (T)_Deserialize(this, typeof(T));

        public T Deserialize<T>() => (T)_Deserialize(this, typeof(T));

        public object Deserialize(Type target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return _Deserialize(this, target);
        }

        internal static object _Deserialize(PacketReader reader, Type type)
        {
            if (type == typeof(object))
                return reader;
            if (reader._Convert(type, out var ret))
                return ret;

            var res = _Caches.SetMethods(type);
            if (res == null)
                throw new PacketException(PacketError.InvalidType);
            var arr = res.args;
            var fun = res.func;
            var obj = new object[arr.Length];

            for (int i = 0; i < arr.Length; i++)
                obj[i] = _Deserialize(reader._GetItem(arr[i].name, false), arr[i].type);
            var val = fun.Invoke(obj);
            return val;
        }
    }
}
