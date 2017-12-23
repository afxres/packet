using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using ItemDirectory = System.Collections.Generic.Dictionary<string, object>;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据包生成工具. Binary packet writer
    /// </summary>
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal object _obj = null;
        internal readonly ConverterDictionary _con = null;

        /// <summary>
        /// 创建对象并指定转换器. Create writer with converters
        /// </summary>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketWriter(ConverterDictionary converters = null) => _con = converters;

        internal ItemDirectory _GetItems()
        {
            if (_obj is ItemDirectory dic)
                return dic;
            var val = new ItemDirectory();
            _obj = val;
            return val;
        }

        /// <summary>
        /// 写入标签和另一个实例. Write key and another instance
        /// </summary>
        public PacketWriter Push(string key, PacketWriter val)
        {
            _GetItems()[key] = new PacketWriter(_con) { _obj = val?._obj };
            return this;
        }

        /// <summary>
        /// 写入标签和原始数据包生成工具. Write key and raw writer
        /// </summary>
        public PacketWriter Push(string key, PacketRawWriter val)
        {
            _GetItems()[key] = val;
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
            _GetItems()[key] = new PacketWriter(_con) { _obj = buf };
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
            _GetItems()[key] = new PacketWriter(_con) { _obj = buf };
            return this;
        }

        /// <summary>
        /// 写入标签和字节数组. Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _GetItems()[key] = nod;
            return this;
        }

        internal PacketWriter _GetBytes(IPacketConverter con, IEnumerable val)
        {
            var hea = con.Length < 1;
            var mst = _GetStream();
            foreach (var i in val)
                mst._WriteOpt(con._GetBytesWrapErr(i), hea);
            _obj = mst.ToArray();
            return this;
        }

        internal PacketWriter _GetBytes<T>(IEnumerable<T> val)
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
                nod._GetBytes(_Caches.Converter(type, _con, false), val);
            _GetItems()[key] = nod;
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
                nod._GetBytes(val);
            _GetItems()[key] = nod;
            return this;
        }

        /// <summary>
        /// 生成数据包. Get binary packet
        /// </summary>
        public byte[] GetBytes()
        {
            if (_obj is byte[] buf)
                return buf;
            var dic = _obj as ItemDirectory;
            if (dic == null)
                return new byte[0];
            var mst = _GetStream();
            _GetBytes(mst, dic, 0);
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
                stb.AppendFormat("{0} node(s)", ((ItemDirectory)_obj).Count);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void _GetBytes(MemoryStream str, ItemDirectory dic, int lev)
        {
            if (lev > _Caches._RecursionDepth)
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
                    var nod = (ItemDirectory)obj;
                    var pos = str.Position;
                    str.Position += sizeof(int);
                    _GetBytes(str, nod, lev + 1);
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

        internal static bool _GetWriter(object val, ConverterDictionary cons, out object value)
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
            else if (val.GetType()._IsImplOfEnumerable(out var inn) && (con = _Caches.Converter(inn, cons, true)) != null)
                wtr = new PacketWriter(cons)._GetBytes(con, (IEnumerable)val);

            value = wtr;
            return wtr != null;
        }

        internal static PacketWriter _Serialize(object val, ConverterDictionary cons, int lev)
        {
            if (lev > _Caches._RecursionDepth)
                throw new PacketException(PacketError.RecursiveError);
            if (_GetWriter(val, cons, out var nod))
                return (PacketWriter)nod;

            var wtr = new PacketWriter(cons);
            var lst = wtr._GetItems();

            if (val is IDictionary<string, object> dic)
                foreach (var i in dic)
                    lst[i.Key] = _Serialize(i.Value, cons, lev + 1);
            else
                foreach (var i in _Caches.GetMethods(val.GetType()))
                    lst[i._name] = _Serialize(i._func.Invoke(val), cons, lev + 1);
            return wtr;
        }

        /// <summary>
        /// 从对象或字典创建对象. Create new writer from object or dictionary (generic dictionary with string as key)
        /// </summary>
        public static PacketWriter Serialize(object obj, ConverterDictionary converters = null) => _Serialize(obj, converters, 0);
    }
}
