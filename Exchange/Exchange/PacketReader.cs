using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using PullFunc = System.Func<byte[], int, int, object>;

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
        internal string _key = null;
        internal Dictionary<string, PacketReader> _dic = null;
        internal Dictionary<Type, PullFunc> _funs = null;

        internal PacketReader(Dictionary<Type, PullFunc> funcs) => _funs = funcs;

        /// <summary>
        /// 创建新的数据包解析器
        /// </summary>
        /// <param name="buffer">待读取的数据包</param>
        /// <param name="funcs">类型转换工具词典 为空时使用默认词典</param>
        public PacketReader(byte[] buffer, Dictionary<Type, PullFunc> funcs = null)
        {
            _buf = buffer;
            _len = buffer.Length;
            _funs = funcs ?? PacketExtensions.PullFuncs();
        }

        internal bool _TryRead()
        {
            if (_dic != null)
                return true;
            var dic = new Dictionary<string, PacketReader>();
            var str = new MemoryStream(_buf) { Position = _off };
            var len = _off + _len;

            while (str.Position < len)
            {
                var rcd = new PacketReader(_funs);
                var key = str.TryReadExt();
                if (key == null)
                    return false;
                var buf = str.TryRead(sizeof(int));
                if (buf == null)
                    return false;
                var tmp = BitConverter.ToInt32(buf, 0);
                if (tmp < 0 || str.Position + tmp > len)
                    return false;
                rcd._buf = _buf;
                rcd._len = tmp;
                rcd._key = Encoding.UTF8.GetString(key);
                rcd._off = (int)str.Position;
                str.Seek(rcd._len, SeekOrigin.Current);
                dic.Add(rcd._key, rcd);
            }

            _dic = dic;
            return true;
        }

        internal PullFunc _Func(Type type, bool nothrow)
        {
            if (_funs.TryGetValue(type, out var fun))
                return fun;
            if (type.IsValueType())
                return (buf, idx, len) => PacketExtensions.GetValue(buf, idx, len, type);
            if (nothrow)
                return null;
            throw new PacketException(PacketError.InvalidType);
        }

        internal PacketReader _Item(string key, bool nothrow)
        {
            if (_TryRead() == false)
                throw new PacketException(PacketError.LengthOverflow);
            if (_dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(PacketError.KeyNotFound);
        }

        internal PacketReader _ItemPath(string path, bool nothrow, string[] separator)
        {
            var sts = path.Split(separator ?? PacketExtensions.GetSeparator(), StringSplitOptions.RemoveEmptyEntries);
            var rdr = this;
            foreach (var i in sts)
                if ((rdr = rdr._Item(i, nothrow)) == null)
                    return null;
            return rdr;
        }

        internal IEnumerable _List(Type type, bool withLengthInfo)
        {
            var inf = type.IsValueType() == false || withLengthInfo == true;
            var fun = new Func<byte[], object>((val) => _Func(type, false).Invoke(val, 0, val.Length));
            var str = new MemoryStream(_buf, _off, _len);
            while (str.Position < str.Length)
            {
                var buf = inf ? str.TryReadExt() : str.TryRead(Marshal.SizeOf(type));
                var tmp = fun.Invoke(buf);
                yield return tmp;
            }
            yield break;
        }

        internal IEnumerable<T> _ListGeneric<T>(bool withLengthInfo)
        {
            foreach (var i in PullList(typeof(T)))
                yield return (T)i;
            yield break;
        }

        /// <summary>
        /// 使用路径访问元素
        /// Get node by path
        /// </summary>
        /// <param name="path">元素路径</param>
        /// <param name="nothrow">失败时返回 null (而不是抛出异常)</param>
        /// <param name="separator">路径分隔符 (为 null 时使用默认)</param>
        public PacketReader this[string path, bool nothrow = false, string[] separator = null] => _ItemPath(path, nothrow, separator);

        /// <summary>
        /// 根据键获取子节点
        /// Get node by key
        /// </summary>
        /// <param name="key">字符串标签</param>
        /// <param name="nothrow">失败时返回 null (而不是抛出异常)</param>
        public PacketReader Pull(string key, bool nothrow = false) => _Item(key, nothrow);

        /// <summary>
        /// 将当前节点转换成目标类型
        /// Convert current node to target type
        /// </summary>
        /// <param name="type">目标类型</param>
        public object Pull(Type type)
        {
            var fun = _Func(type, false);
            var res = fun.Invoke(_buf, _off, _len);
            return res;
        }

        /// <summary>
        /// 将当前节点转换成目标类型
        /// Convert current node to target type
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        public T Pull<T>() => (T)Pull(typeof(T));

        /// <summary>
        /// 将当前节点转换成字节数组
        /// Convert current node to byte array
        /// </summary>
        public byte[] PullList() => _buf.Split(_off, _len);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// Convert current node to collection of target type
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="withLengthInfo">数据是否包含长度信息</param>
        public IEnumerable PullList(Type type, bool withLengthInfo = false) => _List(type, withLengthInfo);

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// Convert current node to collection of target type
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="withLengthInfo">数据是否包含长度信息 (仅针对值类型)</param>
        public IEnumerable<T> PullList<T>(bool withLengthInfo = false) => _ListGeneric<T>(withLengthInfo);

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketReader(parameter, this);
        }

        /// <summary>
        /// 在字符串中输出键值和元素个数
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
    }
}
