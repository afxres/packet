using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static Mikodev.Network.PacketExtensions;
using ItemDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary packet writer
    /// </summary>
    public class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int _Level = 32;
        internal object _obj = null;
        internal Dictionary<Type, PacketConverter> _con = null;

        /// <summary>
        /// Create new writer
        /// </summary>
        /// <param name="converters">Binary converters, use default converters if null</param>
        public PacketWriter(Dictionary<Type, PacketConverter> converters = null)
        {
            _con = converters ?? s_Converters;
        }

        internal PacketConverter _Find(Type type, bool nothrow)
        {
            if (_con.TryGetValue(type, out var con))
                return con;
            if (_GetConverter(type, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(PacketError.TypeInvalid);
        }

        internal ItemDictionary _ItemList()
        {
            if (_obj is ItemDictionary dic)
                return dic;
            var val = new ItemDictionary();
            _obj = val;
            return val;
        }

        internal void _ItemPush(string key, PacketWriter val)
        {
            if (val is null)
                throw new PacketException(PacketError.AssertFailed);
            var dic = _ItemList();
            dic[key] = val;
        }

        /// <summary>
        /// 写入标签和另一个实例
        /// <para>Write key and another instance</para>
        /// </summary>
        public PacketWriter Push(string key, PacketWriter val)
        {
            if (val is null)
                val = new PacketWriter(_con);
            else
                val._con = _con;
            _ItemPush(key, val);
            return this;
        }

        /// <summary>
        /// 写入标签和数据
        /// <para>Write key and data</para>
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push(string key, Type type, object val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._obj = _Find(type, false).ToBinary.Invoke(val);
            _ItemPush(key, nod);
            return this;
        }

        /// <summary>
        /// 写入标签和数据
        /// <para>Write key and data</para>
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push<T>(string key, T val) => Push(key, typeof(T), val);

        /// <summary>
        /// Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _ItemPush(key, nod);
            return this;
        }

        internal void _ByteList(Type type, IEnumerable val)
        {
            if (val is null)
                throw new PacketException(PacketError.AssertFailed);
            var con = _Find(type, false);
            var mst = new MemoryStream();
            foreach (var v in val)
            {
                var buf = con.ToBinary.Invoke(v);
                if (con.Length is int len)
                    if (buf.Length == len)
                        mst._Write(buf);
                    else
                        throw new PacketException(PacketError.Overflow);
                else mst._WriteExt(buf);
            }
            mst.Dispose();
            _obj = mst.ToArray();
        }

        /// <summary>
        /// 写入标签和对象集合
        /// <para>Write key and collections</para>
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList(string key, Type type, IEnumerable val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(type, val);
            _ItemPush(key, nod);
            return this;
        }

        /// <summary>
        /// 写入标签和对象集合
        /// <para>Write key and collections</para>
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> val) => PushList(key, typeof(T), val);

        internal static PacketWriter _ItemNode(object val, Dictionary<Type, PacketConverter> con)
        {
            var fun = default(PacketConverter);
            var wtr = new PacketWriter(con);

            if (val is null)
                wtr._obj = null;
            else if (val is PacketWriter pkt)
                wtr._obj = pkt._obj;
            else if ((fun = wtr._Find(val.GetType(), true)) != null)
                wtr._obj = fun.ToBinary.Invoke(val);
            else if (val.GetType()._IsEnumerable(out var inn))
                wtr._ByteList(inn, (IEnumerable)val);
            else
                return null;
            return wtr;
        }

        internal void _Byte(MemoryStream str, ItemDictionary dic, int lvl)
        {
            if (lvl > _Level)
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
                _Byte(str, (ItemDictionary)val._obj, lvl + 1);
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
            if (_obj is byte[] buf)
                return buf._Split(0, buf.Length);
            var dic = _obj as ItemDictionary;
            if (dic == null)
                return new byte[0];
            var mst = new MemoryStream();
            _Byte(mst, dic, 0);
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
                stb.AppendFormat("{0} node(s)", ((ItemDictionary)_obj).Count);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new DynamicPacketWriter(parameter, this);
        }

        internal static PacketWriter _Serialize(object val, Dictionary<Type, PacketConverter> con, int lvl)
        {
            if (lvl > _Level)
                throw new PacketException(PacketError.RecursiveError);
            var wtr = new PacketWriter(con);
            var nod = default(PacketWriter);

            void _push(string key, object obj)
            {
                var sub = _Serialize(obj, con, lvl + 1);
                wtr._ItemPush(key, sub);
            }

            if (val is IDictionary<string, object> dic)
                foreach (var p in dic)
                    _push(p.Key, p.Value);
            else if ((nod = _ItemNode(val, con)) != null)
                return nod;
            else
                foreach (var p in val.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    _push(p.Name, p.GetValue(val));
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
