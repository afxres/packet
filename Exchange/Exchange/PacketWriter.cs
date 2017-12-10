using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ItemNodes = System.Collections.Generic.Dictionary<string, object>;
using TypeTools = System.Collections.Generic.IReadOnlyDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包生成工具. Binary packet writer
    /// </summary>
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal object _obj = null;
        internal readonly TypeTools _con = null;

        /// <summary>
        /// 创建对象并指定转换器. Create writer with converters
        /// </summary>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketWriter(TypeTools converters = null) => _con = converters;

        internal ItemNodes _ItemList()
        {
            if (_obj is ItemNodes dic)
                return dic;
            var val = new ItemNodes();
            _obj = val;
            return val;
        }

        /// <summary>
        /// 写入标签和另一个实例. Write key and another instance
        /// </summary>
        public PacketWriter Push(string key, PacketWriter val)
        {
            _ItemList()[key] = new PacketWriter(_con) { _obj = val?._obj };
            return this;
        }

        /// <summary>
        /// 写入标签和原始数据包生成工具. Write key and raw writer
        /// </summary>
        public PacketWriter Push(string key, PacketRawWriter val)
        {
            _ItemList()[key] = val;
            return this;
        }

        /// <summary>
        /// 写入标签和数据. Write key and data
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push(string key, Type type, object val)
        {
            var buf = _Caches.GetBytes(type, _con, val);
            _ItemList()[key] = new PacketWriter(_con) { _obj = buf };
            return this;
        }

        /// <summary>
        /// 写入标签和数据 (泛型). Write key and data
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push<T>(string key, T val)
        {
            var buf = _Caches.GetBytes(_con, val);
            _ItemList()[key] = new PacketWriter(_con) { _obj = buf };
            return this;
        }

        /// <summary>
        /// 写入标签和字节数组. Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _ItemList()[key] = nod;
            return this;
        }

        internal PacketWriter _ByteList(IPacketConverter con, IEnumerable val)
        {
            var hea = con.Length < 1;
            var mst = _GetStream();
            foreach (var i in val)
                mst._WriteOpt(con._GetBytesWrapErr(i), hea);
            _obj = mst.ToArray();
            return this;
        }

        internal PacketWriter _ByteList<T>(IEnumerable<T> val)
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var hea = con.Length < 1;
            var mst = _GetStream();
            if (con is IPacketConverter<T> res)
                foreach (var i in val)
                    mst._WriteOpt(res._GetBytesWrapErr(i), hea);
            else
                foreach (var i in val)
                    mst._WriteOpt(con._GetBytesWrapErr(i), hea);
            _obj = mst.ToArray();
            return this;
        }

        /// <summary>
        /// 写入标签和数据集合. Write key and collections
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList(string key, Type type, IEnumerable val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(_Caches.Converter(type, _con, false), val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// 写入标签和数据集合 (泛型). Write key and collections
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList<T>(string key, IEnumerable<T> val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// 生成数据包. Get binary packet
        /// </summary>
        public byte[] GetBytes()
        {
            if (_obj is byte[] buf)
                return buf;
            var dic = _obj as ItemNodes;
            if (dic == null)
                return new byte[0];
            var mst = _GetStream();
            _Byte(mst, dic, 0);
            var res = mst.ToArray();
            return res;
        }

        /// <summary>
        /// 打印对象类型, 子节点个数和字节长度. Show byte count or node count
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
                stb.AppendFormat("{0} node(s)", ((ItemNodes)_obj).Count);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void _Byte(MemoryStream str, ItemNodes dic, int lev)
        {
            if (lev > _Caches._RecDeep)
                throw new PacketException(PacketError.RecursiveError);
            foreach (var i in dic)
            {
                str._WriteExt(Encoding.UTF8.GetBytes(i.Key));
                var val = i.Value;
                if (val is PacketRawWriter raw)
                {
                    str._WriteExt(raw._mst);
                    continue;
                }
                var wtr = (PacketWriter)val;
                var obj = wtr._obj;
                if (obj == null)
                    str._WriteLen(0);
                else if (obj is byte[] buf)
                    str._WriteExt(buf);
                else
                {
                    var nod = (ItemNodes)obj;
                    var pos = str.Position;
                    str.Position += sizeof(int);
                    _Byte(str, nod, lev + 1);
                    var end = str.Position;
                    var len = end - pos - sizeof(int);
                    if (len > int.MaxValue)
                        throw new PacketException(PacketError.Overflow);
                    str.Position = pos;
                    str._WriteLen((int)len);
                    str.Position = end;
                }
            }
        }

        internal static bool _ItemNode(object val, TypeTools cons, out object value)
        {
            var wtr = default(object);
            var con = default(IPacketConverter);

            if (val == null)
                wtr = new PacketWriter(cons);
            else if (val is PacketRawWriter raw)
                wtr = raw;
            else if (val is PacketWriter wri)
                wtr = new PacketWriter(cons) { _obj = wri._obj };
            else if ((con = _Caches.Converter(val.GetType(), cons, true)) != null)
                wtr = new PacketWriter(cons) { _obj = con._GetBytesWrapErr(val) };
            else if (val.GetType()._IsEnumerable(out var inn) && (con = _Caches.Converter(inn, cons, true)) != null)
                wtr = new PacketWriter(cons)._ByteList(con, (IEnumerable)val);

            value = wtr;
            return wtr != null;
        }

        internal static PacketWriter _Serialize(object val, TypeTools cons, int lev)
        {
            if (lev > _Caches._RecDeep)
                throw new PacketException(PacketError.RecursiveError);
            if (_ItemNode(val, cons, out var nod))
                return (PacketWriter)nod;

            var wtr = new PacketWriter(cons);
            var lst = wtr._ItemList();

            if (val is IDictionary<string, object> dic)
                foreach (var i in dic)
                    lst[i.Key] = _Serialize(i.Value, cons, lev + 1);
            else
                foreach (var i in _Caches.GetMethods(val.GetType()))
                    lst[i.Key] = _Serialize(i.Value.Invoke(val), cons, lev + 1);
            return wtr;
        }

        /// <summary>
        /// 从对象或字典创建对象. Create new writer from object or dictionary (generic dictionary with string as key)
        /// </summary>
        public static PacketWriter Serialize(object obj, TypeTools converters = null) => _Serialize(obj, converters, 0);
    }
}
