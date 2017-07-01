using System;
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

        internal void _Read()
        {
            var str = new MemoryStream(_buf);
            str.Position = _off;
            var len = _off + _len;
            while (str.Position < len)
            {
                var rcd = new PacketReader(_funs);
                rcd._buf = _buf;
                rcd._key = Encoding.UTF8.GetString(str.Read(_buf.Length, true));
                var buf = str.Read(sizeof(int));
                rcd._len = BitConverter.ToInt32(buf, 0);
                rcd._off = (int)str.Position;
                str.Seek(rcd._len, SeekOrigin.Current);
                _dic[rcd._key] = rcd;
            }
        }

        internal PacketReader _GetValue(string key, bool nothrow = false)
        {
            if (_dic == null)
            {
                _dic = new Dictionary<string, PacketReader>();
                _Read();
            }
            if (_dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new KeyNotFoundException();
        }

        internal PullFunc _GetFunc(Type type, bool nothrow = false)
        {
            if (_funs.TryGetValue(type, out var fun))
                return fun;
            if (type.IsValueType())
                return (buf, idx, len) => PacketExtensions.GetValue(buf, idx, len, type);
            if (nothrow)
                return null;
            throw new InvalidCastException();
        }

        /// <summary>
        /// 将当前节点转换成目标类型
        /// Convert current node to target type.
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        public T Pull<T>()
        {
            var fun = _GetFunc(typeof(T));
            var res = fun.Invoke(_buf, _off, _len);
            return (T)res;
        }

        /// <summary>
        /// 根据键读取子节点
        /// Get child node by key
        /// </summary>
        /// <param name="key">字符串标签</param>
        public PacketReader Pull(string key) => _GetValue(key);

        /// <summary>
        /// 根据键读取目标类型数据
        /// Get value of target type by key
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="key">字符串标签</param>
        public T Pull<T>(string key)
        {
            var rcd = _GetValue(key);
            var fun = _GetFunc(typeof(T));
            var res = fun.Invoke(_buf, rcd._off, rcd._len);
            return (T)res;
        }

        /// <summary>
        /// 将当前节点转换成字节数组
        /// Convert current node to byte array
        public byte[] PullList()
        {
            return _buf.Split(_off, _len);
        }

        /// <summary>
        /// 根据键读取字节数据
        /// Get byte array by key
        /// </summary>
        public byte[] PullList(string key)
        {
            var rcd = _GetValue(key);
            return _buf.Split(rcd._off, rcd._len);
        }

        /// <summary>
        /// 将当前节点转换成目标类型数据集合
        /// Convert current node to collection of target type
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="withLengthInfo">数据是否包含长度信息 (仅针对值类型)</param>
        public IList<T> PullList<T>(bool withLengthInfo = false)
        {
            var typ = typeof(T);
            var inf = typ.IsValueType() == false || withLengthInfo == true;
            var fun = new Func<byte[], T>((val) => (T)_GetFunc(typ).Invoke(val, 0, val.Length));
            // 读取数据并生成集合
            var lst = new List<T>();
            var str = new MemoryStream(_buf, _off, _len);
            while (str.Position < str.Length)
                lst.Add(fun.Invoke(str.Read(inf ? _buf.Length : Marshal.SizeOf<T>(), inf)));
            return lst;
        }

        /// <summary>
        /// 根据键读取目标类型数据集合
        /// Get collection of target type by key
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="key">字符串标签</param>
        /// <param name="withLengthInfo">数据是否包含长度信息 (仅针对值类型)</param>
        public IList<T> PullList<T>(string key, bool withLengthInfo = false)
        {
            var typ = typeof(T);
            var rcd = _GetValue(key);
            var inf = typ.IsValueType() == false || withLengthInfo == true;
            var fun = new Func<byte[], T>((val) => (T)_GetFunc(typ).Invoke(val, 0, val.Length));
            // 读取数据并生成集合
            var lst = new List<T>();
            var str = new MemoryStream(_buf, rcd._off, rcd._len);
            while (str.Position < str.Length)
                lst.Add(fun.Invoke(str.Read(inf ? _buf.Length : Marshal.SizeOf<T>(), inf)));
            return lst;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketReader(parameter, this);
        }

        /// <summary>
        /// 在字符串中输出键值和元素个数
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(PacketReader)} with key \"{_key ?? "null"}\" and {_dic?.Count ?? 0} element(s)";
        }
    }
}
