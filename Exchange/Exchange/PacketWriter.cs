using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using PushFunc = System.Func<object, byte[]>;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包生成器
    /// </summary>
    public partial class PacketWriter : DynamicObject
    {
        internal byte[] _dat = null;
        internal Dictionary<string, PacketWriter> _dic = null;
        internal Dictionary<Type, PushFunc> _funs = null;

        /// <summary>
        /// 创建新的数据包生成器
        /// </summary>
        /// <param name="funcs">类型转换工具词典 为空时使用默认词典</param>
        public PacketWriter(Dictionary<Type, PushFunc> funcs = null)
        {
            _funs = funcs ?? PacketExtensions.PushFuncs();
        }

        internal PacketWriter _GetValue(string key)
        {
            _dat = null;
            if (_dic == null)
                _dic = new Dictionary<string, PacketWriter>();
            if (_dic.TryGetValue(key, out var val))
                return val;
            val = new PacketWriter();
            _dic.Add(key, val);
            return val;
        }

        internal PushFunc _GetFunc(Type type, bool nothrow = false)
        {
            if (_funs.TryGetValue(type, out var fun))
                return fun;
            if (type.IsValueType())
                return (val) => PacketExtensions.GetBytes(val, type);
            if (nothrow)
                return null;
            throw new InvalidCastException();
        }

        internal PacketWriter _Push(string key, byte[] buffer)
        {
            var val = _GetValue(key);
            val._dic = null;
            val._dat = buffer;
            return this;
        }

        /// <summary>
        /// 写入标签和另一个实例的数据
        /// </summary>
        public PacketWriter Push(string key, PacketWriter other)
        {
            _dic[key] = other;
            return this;
        }

        /// <summary>
        /// 写入标签和数据
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="key">字符串标签</param>
        /// <param name="value">待写入数据</param>
        public PacketWriter Push<T>(string key, T value)
        {
            var buf = _GetFunc(typeof(T)).Invoke(value);
            return _Push(key, buf);
        }

        /// <summary>
        /// 写入标签和字节数据
        /// </summary>
        public PacketWriter PushList(string key, byte[] buffer) => _Push(key, buffer);

        /// <summary>
        /// 写入标签和对象集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">标签</param>
        /// <param name="value">数据集合</param>
        /// <param name="withLengthInfo">是否写入长度信息 (仅针对值类型)</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> value, bool withLengthInfo = false)
        {
            var typ = typeof(T);
            var inf = withLengthInfo || typ.IsValueType() == false;
            var str = new MemoryStream();
            var fun = _GetFunc(typeof(T));
            foreach (var v in value)
                str.Write(fun.Invoke(v), inf);
            return _Push(key, str.ToArray());
        }

        internal byte[] _GetBytes(Dictionary<string, PacketWriter> dic)
        {
            var str = new MemoryStream();
            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                str.Write(key.GetBytes(), true);
                if (val._dat != null)
                    str.Write(val._dat, true);
                else if (val._dic != null)
                    str.Write(_GetBytes(val._dic), true);
                else
                    str.Write(0);
            }
            return str.ToArray();
        }

        /// <summary>
        /// 生成数据包
        /// </summary>
        public byte[] GetBytes()
        {
            if (_dic != null)
                return _GetBytes(_dic);
            return new byte[0];
        }

        /// <summary>
        /// 动态创建对象
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _GetValue(binder.Name);
            return true;
        }

        /// <summary>
        /// 动态写入对象 不支持集合
        /// </summary>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var key = binder.Name;
            if (value == null)
                return false;

            if (value is byte[] dat)
            {
                _Push(key, dat);
                return true;
            }

            var fun = _GetFunc(value.GetType(), true);
            if (fun == null)
                return false;
            _Push(key, fun.Invoke(value));
            return true;
        }

        /// <summary>
        /// 在字符串中输出键值和元素
        /// </summary>
        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (_dat != null)
                stb.AppendFormat("{0} byte(s)", _dat.Length);
            else if (_dic != null)
                stb.AppendFormat("{0} element(s)", _dic.Count);
            else
                stb.Append("none");
            return stb.ToString();
        }
    }
}
