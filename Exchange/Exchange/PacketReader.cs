using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using static Mikodev.Network.PacketExtensions;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包解析器
    /// </summary>
    public partial class PacketReader : IDynamicMetaObjectProvider
    {
        internal int _off = 0;
        internal int _len = 0;
        internal byte[] _buf = null;
        internal Dictionary<string, PacketReader> _dic = null;
        internal Dictionary<Type, PacketConverter> _funs = null;

        internal PacketReader(byte[] buffer, int offset, int length, Dictionary<Type, PacketConverter> converters)
        {
            _buf = buffer;
            _off = offset;
            _len = length;
            _funs = converters;
        }

        /// <summary>
        /// 创建新的数据包解析器
        /// </summary>
        /// <param name="buffer">待读取的数据包</param>
        /// <param name="converters">类型转换工具词典 为空时使用默认词典</param>
        public PacketReader(byte[] buffer, Dictionary<Type, PacketConverter> converters = null)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _len = buffer.Length;
            _funs = converters ?? s_Converters;
        }

        internal bool _ReadBuffer()
        {
            if (_dic != null)
                return true;
            var dic = new Dictionary<string, PacketReader>();
            var str = new MemoryStream(_buf) { Position = _off };
            var max = _off + _len;

            while (str.Position < max)
            {
                var kbf = str._ReadExt();
                if (kbf == null)
                    return false;
                var buf = str._Read(sizeof(int));
                if (buf == null)
                    return false;
                var len = BitConverter.ToInt32(buf, 0);
                if (len < 0 || str.Position + len > max)
                    return false;
                var key = Encoding.UTF8.GetString(kbf);
                var off = (int)str.Position;
                str.Seek(len, SeekOrigin.Current);
                dic.Add(key, new PacketReader(_buf, off, len, _funs));
            }

            _dic = dic;
            return true;
        }

        internal PacketConverter _Convert(Type type, bool nothrow)
        {
            if (_funs.TryGetValue(type, out var fun))
                return fun;
            if (_GetConverter(type, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(PacketError.TypeInvalid);
        }

        internal PacketReader _Item(string key, bool nothrow)
        {
            if (_ReadBuffer() == false)
                throw new PacketException(PacketError.Overflow);
            if (_dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(PacketError.PathError);
        }

        internal PacketReader _ItemPath(string path, bool nothrow, string[] separator)
        {
            var sts = path.Split(separator ?? s_Separators, StringSplitOptions.RemoveEmptyEntries);
            var rdr = this;
            foreach (var i in sts)
                if ((rdr = rdr._Item(i, nothrow)) == null)
                    return null;
            return rdr;
        }

        internal IEnumerable _List(Type type)
        {
            var con = _Convert(type, false);
            var str = new MemoryStream(_buf, _off, _len);
            while (str.Position < str.Length)
            {
                var buf = con.Length == null ? str._ReadExt() : str._Read((int)con.Length);
                var tmp = con.ObjectFunction.Invoke(buf, 0, buf.Length);
                yield return tmp;
            }
            str.Dispose();
            yield break;
        }

        internal IEnumerable<T> _ListGeneric<T>()
        {
            foreach (var i in _List(typeof(T)))
                yield return (T)i;
            yield break;
        }

        internal IEnumerable<string> _Keys()
        {
            if (_ReadBuffer() == false)
                yield break;
            foreach (var i in _dic)
                yield return i.Key;
            yield break;
        }

        /// <summary>
        /// Child node count
        /// </summary>
        public int Count => _ReadBuffer() ? _dic.Count : 0;

        /// <summary>
        /// Child node keys
        /// </summary>
        public IEnumerable<string> Keys => _Keys();

        /// <summary>
        /// 使用路径访问元素
        /// <para>Get node by path</para>
        /// </summary>
        /// <param name="path">元素路径</param>
        /// <param name="nothrow">失败时返回 null (而不是抛出异常)</param>
        /// <param name="separator">路径分隔符 (为 null 时使用默认)</param>
        public PacketReader this[string path, bool nothrow = false, string[] separator = null] => _ItemPath(path, nothrow, separator);

        /// <summary>
        /// 根据键获取子节点
        /// <para>Get node by key</para>
        /// </summary>
        /// <param name="key">字符串标签</param>
        /// <param name="nothrow">失败时返回 null (而不是抛出异常)</param>
        public PacketReader Pull(string key, bool nothrow = false) => _Item(key, nothrow);

        /// <summary>
        /// 将当前节点转换成目标类型
        /// <para>Convert current node to target type</para>
        /// </summary>
        /// <param name="type">目标类型</param>
        public object Pull(Type type)
        {
            var con = _Convert(type, false);
            var res = con.ObjectFunction.Invoke(_buf, _off, _len);
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
        public byte[] PullList() => _buf._Split(_off, _len);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// <para>Convert current node to target type collection</para>
        /// </summary>
        /// <param name="type">目标类型</param>
        public IEnumerable PullList(Type type) => _List(type);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// <para>Convert current node to target type collection</para>
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        public IEnumerable<T> PullList<T>() => _ListGeneric<T>();

        /// <summary>
        /// 显示字节长度或节点个数
        /// <para>Show byte count or node count</para>
        /// </summary>
        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            if (_dic == null || _dic.Count < 1)
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
