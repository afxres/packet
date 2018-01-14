using System;
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
        internal readonly ConverterDictionary _cvt = null;
        internal Dictionary<string, PacketReader> _itm = null;
        internal _Element _spa;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer);
            _cvt = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _cvt = converters;
        }

        internal bool _Init()
        {
            if (_itm != null)
                return true;
            if (_spa._idx != _spa._off)
                return false;
            var dic = new Dictionary<string, PacketReader>();
            var len = 0;

            while (_spa._idx < _spa._max)
            {
                if (_spa._buf._HasNext(_spa._max, ref _spa._idx, out len) == false)
                    return false;
                var key = Encoding.UTF8.GetString(_spa._buf, _spa._idx, len);
                if (dic.ContainsKey(key))
                    return false;
                _spa._idx += len;
                if (_spa._buf._HasNext(_spa._max, ref _spa._idx, out len) == false)
                    return false;
                dic.Add(key, new PacketReader(_spa._buf, _spa._idx, len, _cvt));
                _spa._idx += len;
            }

            _itm = dic;
            return true;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader _GetItem(string key, bool nothrow)
        {
            var res = _Init();
            if (res == true && _itm.TryGetValue(key, out var val))
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
                if ((rdr = rdr._GetItem(i ?? throw new ArgumentNullException(), nothrow)) == null)
                    return null;
            return rdr;
        }

        public int Count => _Init() ? _itm.Count : 0;

        public IEnumerable<string> Keys => _Init() ? _itm.Keys : Enumerable.Empty<string>();

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
            if (_Init())
                stb.AppendFormat("{0} node(s), ", _itm.Count);
            stb.AppendFormat("{0} byte(s)", _spa._len);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        internal bool _Convert(Type type, out object value)
        {
            var val = default(object);
            var con = default(IPacketConverter);
            var det = default(_DetailInfo);

            if (type == typeof(PacketReader))
                val = this;
            else if (type == typeof(PacketRawReader))
                val = new PacketRawReader(this);
            else if ((con = _Caches.GetConverter(_cvt, type, true)) != null)
                val = con._GetValueWrapError(_spa._buf, _spa._off, _spa._len, true);
            else if ((det = _Caches.GetDetail(type)).is_arr)
                val = _Caches.GetArray(this, det.arg_of_arr);
            else if (det.is_itr)
                val = _Caches.GetEnumerable(this, det.arg_of_itr);
            else if (det.is_lst)
                val = _Caches.GetList(this, det.arg_of_lst);
            else goto fail;

            value = val;
            return true;

            fail:
            value = null;
            return false;
        }

        public T Deserialize<T>(T anonymous) => (T)_Deserialize(this, typeof(T));

        public T Deserialize<T>() => (T)_Deserialize(this, typeof(T));

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return _Deserialize(this, type);
        }

        internal static object _Deserialize(PacketReader reader, Type type)
        {
            if (type == typeof(object))
                return reader;
            if (reader._Convert(type, out var ret))
                return ret;

            var res = _Caches.GetSetMethods(type);
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
