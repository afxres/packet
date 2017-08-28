using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
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
        internal readonly Dictionary<Type, PacketConverter> _con = null;

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="converters">Binary converters, use default converters if null</param>
        public PacketReader(byte[] buffer, Dictionary<Type, PacketConverter> converters = null)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _len = buffer.Length;
            _con = converters ?? PacketExtensions.s_Converters;
        }

        /// <summary>
        /// Create new reader
        /// </summary>
        /// <param name="buffer">Binary data packet (Should be readonly)</param>
        /// <param name="offset">Start index</param>
        /// <param name="length">Packet length</param>
        /// <param name="converters">Binary converters, use default converters if null</param>
        public PacketReader(byte[] buffer, int offset, int length, Dictionary<Type, PacketConverter> converters = null)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _len = length;
            _con = converters ?? PacketExtensions.s_Converters;
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

        internal PacketReader _ItemPath(string path, bool nothrow, string[] separator)
        {
            var sts = path.Split(separator ?? PacketExtensions.s_Separators, StringSplitOptions.RemoveEmptyEntries);
            var rdr = this;
            foreach (var i in sts)
                if ((rdr = rdr._Item(i, nothrow)) == null)
                    return null;
            return rdr;
        }

        internal IEnumerable _List(Type type)
        {
            if (_con.TryGetValue(type, out var con) == false && PacketCaches.TryGetValue(type, out con) == false)
                throw new PacketException(PacketError.TypeInvalid);

            var max = _off + _len;
            if (con.Length is int len)
                for (int idx = _off; idx < max && idx + len <= max; idx += len)
                    yield return con.ToObject(_buf, idx, len);
            else
                for (int idx = _off; idx < max && _buf._Read(ref idx, out var val, max); idx += val)
                    yield return con.ToObject(_buf, idx, val);
            yield break;
        }

        internal IEnumerable<T> _ListGen<T>()
        {
            foreach (var i in _List(typeof(T)))
                yield return (T)i;
            yield break;
        }

        internal IEnumerable<string> _Keys()
        {
            if (_Init() == true)
                foreach (var i in _dic)
                    yield return i.Key;
            yield break;
        }

        /// <summary>
        /// Child node count
        /// </summary>
        public int Count => _Init() ? _dic.Count : 0;

        /// <summary>
        /// Child node keys
        /// </summary>
        public IEnumerable<string> Keys => _Keys();

        /// <summary>
        /// 使用路径访问元素
        /// <para>Get node by path</para>
        /// </summary>
        /// <param name="path">Node path</param>
        /// <param name="nothrow">return null if path not found</param>
        /// <param name="separator">Path separators, use default separators if null</param>
        [IndexerName("Node")]
        public PacketReader this[string path, bool nothrow = false, string[] separator = null] => _ItemPath(path, nothrow, separator);

        /// <summary>
        /// 根据键获取子节点
        /// <para>Get node by key</para>
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="nothrow">return null if key not found</param>
        public PacketReader Pull(string key, bool nothrow = false) => _Item(key, nothrow);

        /// <summary>
        /// 将当前节点转换成目标类型
        /// <para>Convert current node to target type</para>
        /// </summary>
        /// <param name="type">Target type</param>
        public object Pull(Type type)
        {
            if (_con.TryGetValue(type, out var con) == false && PacketCaches.TryGetValue(type, out con) == false)
                throw new PacketException(PacketError.TypeInvalid);

            var res = con.ToObject(_buf, _off, _len);
            return res;
        }

        /// <summary>
        /// 将当前节点转换成目标类型
        /// <para>Convert current node to target type</para>
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        public T Pull<T>() => (T)Pull(typeof(T));

        /// <summary>
        /// Get byte array of current node
        /// </summary>
        public byte[] PullList() => _buf._Part(_off, _len);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// <para>Convert current node to target type collection</para>
        /// </summary>
        /// <param name="type">Target type</param>
        public IEnumerable PullList(Type type) => _List(type);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// <para>Convert current node to target type collection</para>
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        public IEnumerable<T> PullList<T>() => _ListGen<T>();

        /// <summary>
        /// 显示字节长度或节点个数
        /// <para>Show byte count or node count</para>
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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketReader(parameter, this);
        }
    }
}
