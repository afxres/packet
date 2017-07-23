using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static Mikodev.Network.PacketExtensions;
using WriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包生成器
    /// </summary>
    public partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int _Level = 32;
        internal object _obj = null;
        internal Dictionary<Type, PacketConverter> _con = null;

        /// <summary>
        /// 创建新的数据包生成器
        /// </summary>
        /// <param name="converters">转换器词典 为空时使用默认词典</param>
        public PacketWriter(Dictionary<Type, PacketConverter> converters = null)
        {
            _con = converters ?? s_Converters;
        }

        internal PacketConverter _Convert(Type type, bool nothrow = false)
        {
            if (_con.TryGetValue(type, out var con))
                return con;
            if (_GetConverter(type, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(PacketError.TypeInvalid);
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

        internal PacketWriter _ItemBuffer(string key, byte[] buffer)
        {
            var val = _Item(key);
            val._obj = buffer;
            return this;
        }

        /// <summary>
        /// 写入标签和另一个实例
        /// <para>Write key and another instance</para>
        /// </summary>
        public PacketWriter Push(string key, PacketWriter other)
        {
            _Item(key, other);
            return this;
        }

        /// <summary>
        /// 写入标签和数据
        /// <para>Write key and data</para>
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="key">字符串标签</param>
        /// <param name="value">待写入数据</param>
        public PacketWriter Push<T>(string key, T value) => Push(key, typeof(T), value);

        /// <summary>
        /// 写入标签和数据
        /// <para>Write key and data</para>
        /// </summary>
        /// <param name="key">字符串标签</param>
        /// <param name="type">目标类型</param>
        /// <param name="value">待写入数据</param>
        public PacketWriter Push(string key, Type type, object value)
        {
            if (value == null)
                return _ItemBuffer(key, null);
            var fun = _Convert(type);
            var buf = fun.BinaryFunction.Invoke(value);
            return _ItemBuffer(key, buf);
        }

        /// <summary>
        /// Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buffer) => _ItemBuffer(key, buffer);

        /// <summary>
        /// 写入标签和对象集合
        /// <para>Write key and collections</para>
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">标签</param>
        /// <param name="value">数据集合</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> value) => PushList(key, typeof(T), value);

        /// <summary>
        /// 写入标签和对象集合
        /// <para>Write key and collections</para>
        /// </summary>
        /// <param name="key">标签</param>
        /// <param name="type">对象类型</param>
        /// <param name="value">数据集合</param>
        public PacketWriter PushList(string key, Type type, IEnumerable value)
        {
            if (value == null)
                return _Item(key, null);
            var con = _Convert(type);
            var mst = new MemoryStream();
            foreach (var v in value)
            {
                var buf = con.BinaryFunction.Invoke(v);
                if (con.Length is int len)
                    if (buf.Length == len)
                        mst._Write(buf);
                    else
                        throw new PacketException(PacketError.Overflow);
                else mst._WriteExt(buf);
            }
            mst.Dispose();
            return _ItemBuffer(key, mst.ToArray());
        }

        internal bool _ItemValue(string key, object val)
        {
            var fun = default(PacketConverter);
            var typ = val?.GetType();

            if (val is null)
                _ItemBuffer(key, null);
            else if (val is PacketWriter pkt)
                _Item(key, pkt);
            else if ((fun = _Convert(typ, true)) != null)
                _ItemBuffer(key, fun.BinaryFunction.Invoke(val));
            else if (typ._IsEnumerable(out var inn))
                PushList(key, inn, (IEnumerable)val);
            else
                return false;
            return true;
        }

        internal void _Bytes(MemoryStream str, WriterDictionary dic, int level)
        {
            if (level > _Level)
                throw new PacketException(PacketError.RecursiveError);
            foreach (var i in dic)
            {
                str._WriteExt(Encoding.UTF8.GetBytes(i.Key));
                var val = i.Value;
                if (val._obj is null)
                {
                    str._Write(0);
                    continue;
                }
                if (val._obj is byte[] buf)
                {
                    str._WriteExt(buf);
                    continue;
                }

                var pos = str.Position;
                str._Write(0);
                _Bytes(str, (WriterDictionary)val._obj, level + 1);
                var end = str.Position;
                str.Seek(pos, SeekOrigin.Begin);
                str._Write((int)(end - pos - sizeof(int)));
                str.Seek(end, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// 生成数据包
        /// <para>Get binary packet</para>
        /// </summary>
        public byte[] GetBytes()
        {
            var dic = _obj as WriterDictionary;
            if (dic == null)
                return new byte[0];
            var mst = new MemoryStream();
            _Bytes(mst, dic, 0);
            return mst.ToArray();
        }

        /// <summary>
        /// 显示字节长度或节点个数
        /// <para>Show byte count or node count</para>
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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketWriter(parameter, this);
        }

        internal static PacketWriter _Serialize(object value, Dictionary<Type, PacketConverter> converters, int level)
        {
            if (level > _Level)
                throw new PacketException(PacketError.RecursiveError);
            var wtr = new PacketWriter(converters);

            void _pushItem(string key, object val)
            {
                if (val is IDictionary<string, object> == false && wtr._ItemValue(key, val) == true)
                    return;
                var wri = _Serialize(val, converters, level + 1);
                wtr._Item(key, wri);
            }

            if (value is IDictionary<string, object> dic)
                foreach (var p in dic)
                    _pushItem(p.Key, p.Value);
            else
                foreach (var p in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    _pushItem(p.Name, p.GetValue(value));
            return wtr;
        }

        /// <summary>
        /// 序列化对象或词典
        /// <para>Serialize <see cref="object"/> or <see cref="IDictionary"/></para>
        /// </summary>
        public static PacketWriter Serialize(object obj, Dictionary<Type, PacketConverter> converters = null)
        {
            return _Serialize(obj, converters ?? s_Converters, 0);
        }
    }
}
