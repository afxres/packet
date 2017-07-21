using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using PushFunc = System.Func<object, byte[]>;
using WriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包生成器
    /// </summary>
    public partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal object _obj = null;
        internal Dictionary<Type, PushFunc> _funs = null;

        /// <summary>
        /// 创建新的数据包生成器
        /// </summary>
        /// <param name="funcs">类型转换工具词典 为空时使用默认词典</param>
        public PacketWriter(Dictionary<Type, PushFunc> funcs = null)
        {
            _funs = funcs ?? PacketExtensions.PushFuncs();
        }

        internal PushFunc _Func(Type type, bool nothrow = false)
        {
            if (_funs.TryGetValue(type, out var fun))
                return fun;
            if (type.IsValueType())
                return (val) => PacketExtensions.GetBytes(val, type);
            if (nothrow)
                return null;
            throw new PacketException(PacketError.InvalidType);
        }

        internal PacketWriter _Item(string key, PacketWriter another = null)
        {
            if (_obj is WriterDictionary == false)
                _obj = new WriterDictionary();
            var dic = (WriterDictionary)_obj;
            if (dic.TryGetValue(key, out var val))
                return val;
            val = another ?? new PacketWriter();
            dic.Add(key, val);
            return val;
        }

        internal PacketWriter _Push(string key, byte[] buffer)
        {
            var val = _Item(key);
            val._obj = buffer;
            return this;
        }

        /// <summary>
        /// 写入标签和另一个实例的数据
        /// Write key and another instance
        /// </summary>
        public PacketWriter Push(string key, PacketWriter other)
        {
            _Item(key, other);
            return this;
        }

        /// <summary>
        /// 写入标签和数据
        /// Write key and data
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="key">字符串标签</param>
        /// <param name="value">待写入数据</param>
        public PacketWriter Push<T>(string key, T value) => Push(key, typeof(T), value);

        /// <summary>
        /// 写入标签和数据
        /// Write key and data
        /// </summary>
        /// <param name="key">字符串标签</param>
        /// <param name="type">目标类型</param>
        /// <param name="value">待写入数据</param>
        public PacketWriter Push(string key, Type type, object value)
        {
            if (value == null)
                return _Push(key, null);
            var fun = _Func(type);
            var buf = fun.Invoke(value);
            return _Push(key, buf);
        }

        /// <summary>
        /// 写入标签和对象集合
        /// Write key and collections
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">标签</param>
        /// <param name="value">数据集合</param>
        /// <param name="withLengthInfo">是否写入长度信息 (仅针对值类型)</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> value, bool withLengthInfo = false) => PushList(key, typeof(T), value, withLengthInfo);

        /// <summary>
        /// 写入标签和对象集合
        /// Write key and collections
        /// </summary>
        /// <param name="key">标签</param>
        /// <param name="type">对象类型</param>
        /// <param name="value">数据集合</param>
        /// <param name="withLengthInfo">是否写入长度信息 (仅针对值类型)</param>
        public PacketWriter PushList(string key, Type type, IEnumerable value, bool withLengthInfo = false)
        {
            if (value == null)
                return _Push(key, null);
            var inf = withLengthInfo || type.IsValueType() == false;
            var str = new MemoryStream();
            var fun = _Func(type);
            foreach (var v in value)
            {
                var val = fun.Invoke(v);
                if (inf) str.WriteExt(val);
                else str.Write(val);
            }
            return _Push(key, str.ToArray());
        }

        internal bool _PushValue(string key, object val, Type typ = null)
        {
            var fun = default(PushFunc);
            typ = typ ?? val?.GetType();

            if (val is null)
                _Push(key, null);
            else if (val is PacketWriter pkt)
                _Item(key, pkt);
            else if ((fun = _Func(typ, true)) != null)
                _Push(key, fun.Invoke(val));
            else if (typ.IsEnumerable(out var inn))
                PushList(key, inn, (IEnumerable)val);
            else
                return false;
            return true;
        }

        /// <summary>
        /// 使用现有值象创建新对象 忽略所有无法序列化的对象
        /// </summary>
        public static PacketWriter Serialize(object obj, Dictionary<Type, PushFunc> funcs = null)
        {
            const int _Level = 32;
            PacketWriter _push(object value, int level)
            {
                if (level > _Level)
                    throw new PacketException(PacketError.RecursiveError);
                var wtr = new PacketWriter(funcs);
                var vtp = value.GetType();
                foreach (var p in vtp.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var key = p.Name;
                    var val = p.GetValue(value);

                    if (wtr._PushValue(key, val, p.PropertyType) == true)
                        continue;

                    var wri = _push(val, level + 1);
                    wtr._Item(key, wri);
                }
                return wtr;
            }
            return _push(obj, 0);
        }

        internal void _GetBytes(MemoryStream str, WriterDictionary dic)
        {
            foreach (var i in dic)
            {
                str.WriteExt(Encoding.UTF8.GetBytes(i.Key));
                var val = i.Value;
                if (val._obj is null)
                {
                    str.Write(0);
                    continue;
                }
                if (val._obj is byte[] buf)
                {
                    str.WriteExt(buf);
                    continue;
                }

                var pos = str.Position;
                str.Write(0);
                _GetBytes(str, (WriterDictionary)val._obj);
                var end = str.Position;
                str.Seek(pos, SeekOrigin.Begin);
                str.Write((int)(end - pos - sizeof(int)));
                str.Seek(end, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// 生成数据包
        /// Generate a new packet of byte array form
        /// </summary>
        public byte[] GetBytes()
        {
            var dic = _obj as WriterDictionary;
            if (dic == null)
                return new byte[0];
            var mst = new MemoryStream();
            _GetBytes(mst, dic);
            return mst.ToArray();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketWriter(parameter, this);
        }

        /// <summary>
        /// 在字符串中输出键值和元素
        /// </summary>
        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (_obj is null)
                stb.Append("none");
            else if (_obj is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else
                stb.AppendFormat("{0} node(s)", ((WriterDictionary)_obj).Count);
            return stb.ToString();
        }
    }
}
