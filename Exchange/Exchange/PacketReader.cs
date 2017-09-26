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
        internal readonly int _off = 0;
        internal readonly int _len = 0;
        internal readonly byte[] _buf = null;
        internal Dictionary<string, PacketReader> _dic = null;
        internal readonly Dictionary<Type, IPacketConverter> _con = null;

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketReader(byte[] buffer, Dictionary<Type, IPacketConverter> converters = null)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _len = buffer.Length;
            _con = converters;
        }

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="offset">Start index</param>
        /// <param name="length">Packet length</param>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketReader(byte[] buffer, int offset, int length, Dictionary<Type, IPacketConverter> converters = null)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _len = length;
            _con = converters;
        }

        /// <summary>
        /// Parse this packet (return false if error)
        /// </summary>
        internal bool _Init()
        {
            if (_dic != null)
                return true;
            var dic = new Dictionary<string, PacketReader>();
            var max = _off + _len;
            var idx = _off;
            var len = 0;

            while (idx < max)
            {
                if (_buf._Read(ref idx, out len) == false)
                    return false;
                var key = Encoding.UTF8.GetString(_buf, idx, len);
                idx += len;
                if (_buf._Read(ref idx, out len) == false)
                    return false;
                dic.Add(key, new PacketReader(_buf, idx, len, _con));
                idx += len;
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

        internal PacketReader _ItemPath(string path, bool nothrow, string[] separator)
        {
            var sts = path.Split(separator ?? _Extension.s_seps, StringSplitOptions.RemoveEmptyEntries);
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
        public PacketReader this[string path, bool nothrow = false, string[] separator = null] => _ItemPath(path, nothrow, separator);

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
        public object Pull(Type type) => _Caches.Converter(type, _con, false).GetValue(_buf, _off, _len);

        /// <summary>
        /// Convert current node to target type.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            if (con is IPacketConverter<T> val)
                return val.GetValue(_buf, _off, _len);
            return (T)con.GetValue(_buf, _off, _len);
        }

        /// <summary>
        /// Get byte array of current node
        /// </summary>
        public byte[] PullList() => _buf._ToBytes(_off, _len);

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
                if (_len != 0)
                    stb.AppendFormat("{0} byte(s)", _len);
                else
                    stb.Append("none");
            else
                stb.AppendFormat("{0} node(s)", _dic.Count);
            return stb.ToString();
        }
    }
}
