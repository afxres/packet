using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary packet reader
    /// </summary>
    public class PacketReader : IDynamicMetaObjectProvider
    {
        internal _Span _spa;
        internal Dictionary<string, PacketReader> _dic = null;
        internal readonly IReadOnlyDictionary<Type, IPacketConverter> _con = null;

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketReader(byte[] buffer, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Span(buffer);
            _con = converters;
        }

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="offset">Start index</param>
        /// <param name="length">Packet length</param>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketReader(byte[] buffer, int offset, int length, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Span(buffer, offset, length);
            _con = converters;
        }

        /// <summary>
        /// Parse this packet (return false if error)
        /// </summary>
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
                if (_spa._buf._Read(ref _spa._idx, out len) == false)
                    return false;
                var key = Encoding.UTF8.GetString(_spa._buf, _spa._idx, len);
                _spa._idx += len;
                if (_spa._buf._Read(ref _spa._idx, out len) == false)
                    return false;
                dic.Add(key, new PacketReader(_spa._buf, _spa._idx, len, _con));
                _spa._idx += len;
            }

            _dic = dic;
            return true;
        }

        internal PacketReader _Item(string key, bool nothrow)
        {
            var res = _Init();
            if (res == true && _dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(res ? PacketError.PathError : PacketError.Overflow);
        }

        internal PacketReader _ItemPath(string[] keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr._Item(i, nothrow)) == null)
                    return null;
            return rdr;
        }

        internal PacketReader _ItemPath(string path, bool nothrow, char[] separator)
        {
            var sts = path.Split(separator ?? _Extension.s_seps);
            var res = _ItemPath(sts, nothrow);
            return res;
        }

        /// <summary>
        /// Child node count
        /// </summary>
        public int Count => _Init() ? _dic.Count : 0;

        /// <summary>
        /// Child node keys
        /// </summary>
        public IEnumerable<string> Keys => _Init() ? _dic.Keys : Enumerable.Empty<string>();

        /// <summary>
        /// Get node by path
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="nothrow">return null if path not found</param>
        /// <param name="separator">Path separators, use default separators if null</param>
        [IndexerName("Node")]
        public PacketReader this[string path, bool nothrow = false, char[] separator = null] => _ItemPath(path, nothrow, separator);

        /// <summary>
        /// Get node by key
        /// </summary>
        /// <param name="key">Node key</param>
        /// <param name="nothrow">return null if key not found</param>
        public PacketReader Pull(string key, bool nothrow = false) => _Item(key, nothrow);

        /// <summary>
        /// Get node by keys
        /// </summary>
        /// <param name="keys">Node key list</param>
        /// <param name="nothrow">return null if key not found</param>
        public PacketReader Pull(string[] keys, bool nothrow = false) => _ItemPath(keys, nothrow);

        /// <summary>
        /// Convert current node to target type
        /// </summary>
        /// <param name="type">Target type</param>
        public object Pull(Type type) => _Caches.Converter(type, _con, false).GetValue(_spa._buf, _spa._off, _spa._len);

        /// <summary>
        /// Convert current node to target type.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            if (con is IPacketConverter<T> val)
                return val.GetValue(_spa._buf, _spa._off, _spa._len);
            return (T)con.GetValue(_spa._buf, _spa._off, _spa._len);
        }

        /// <summary>
        /// Get byte array of current node
        /// </summary>
        public byte[] PullList() => _spa._buf._ToBytes(_spa._off, _spa._len);

        /// <summary>
        /// Convert current node to target type collection
        /// </summary>
        /// <param name="type">Target type</param>
        public IEnumerable PullList(Type type) => new _Enumerable(this, type);

        /// <summary>
        /// Convert current node to target type collection
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        public IEnumerable<T> PullList<T>() => new _Enumerable<T>(this);

        /// <summary>
        /// Create dynamic reader
        /// </summary>
        public DynamicMetaObject GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        /// <summary>
        /// Show byte count or node count
        /// </summary>
        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            if (_Init() == false || _dic.Count < 1)
                if (_spa._len != 0)
                    stb.AppendFormat("{0} byte(s)", _spa._len);
                else
                    stb.Append("none");
            else
                stb.AppendFormat("{0} node(s)", _dic.Count);
            return stb.ToString();
        }
    }
}
