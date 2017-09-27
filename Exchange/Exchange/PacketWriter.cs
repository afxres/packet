using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ItemDictionary = System.Collections.Generic.Dictionary<string, object>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary packet writer
    /// </summary>
    public class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int _Level = 64;
        internal object _obj = null;
        internal readonly IReadOnlyDictionary<Type, IPacketConverter> _con = null;

        /// <summary>
        /// Create new writer
        /// </summary>
        /// <param name="converters">Packet converters, use default converters if null</param>
        public PacketWriter(IReadOnlyDictionary<Type, IPacketConverter> converters = null) => _con = converters;

        internal ItemDictionary _ItemList()
        {
            if (_obj is ItemDictionary dic)
                return dic;
            var val = new ItemDictionary();
            _obj = val;
            return val;
        }

        /// <summary>
        /// Write key and another instance
        /// </summary>
        public PacketWriter Push(string key, PacketWriter val)
        {
            _ItemList()[key] = new PacketWriter(_con) { _obj = val?._obj };
            return this;
        }

        /// <summary>
        /// Write key and data
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push(string key, Type type, object val)
        {
            var nod = new PacketWriter(_con);
            var con = _Caches.Converter(type, _con, false);
            nod._obj = con.GetBytes(val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Write key and data
        /// </summary>
        /// <typeparam name="T">Source type</typeparam>
        /// <param name="key">Node tag</param>
        /// <param name="val">Value to be written</param>
        public PacketWriter Push<T>(string key, T val)
        {
            var nod = new PacketWriter(_con);
            var con = _Caches.Converter(typeof(T), _con, false);
            if (con is IPacketConverter<T> res)
                nod._obj = res.GetBytes(val);
            else
                nod._obj = con.GetBytes(val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Set key and byte array
        /// </summary>
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _ItemList()[key] = nod;
            return this;
        }

        internal void _ByteList(Type type, Action<MemoryStream, IPacketConverter, bool> action)
        {
            var con = _Caches.Converter(type, _con, false);
            var mst = new MemoryStream();
            action.Invoke(mst, con, con.Length == null);
            mst.Dispose();
            _obj = mst.ToArray();
        }

        internal PacketWriter _ByteList(Type type, IEnumerable val)
        {
            _ByteList(type, (mst, con, hea) =>
            {
                foreach (var i in val)
                    mst._WriteExt(con.GetBytes(i), hea);
            });
            return this;
        }

        internal PacketWriter _ByteList<T>(IEnumerable<T> val)
        {
            _ByteList(typeof(T), (mst, con, hea) =>
            {
                if (con is IPacketConverter<T> res)
                    foreach (var i in val)
                        mst._WriteExt(res.GetBytes(i), hea);
                else
                    foreach (var i in val)
                        mst._WriteExt(con.GetBytes(i), hea);
            });
            return this;
        }

        /// <summary>
        /// Write key and collections
        /// </summary>
        /// <param name="key">Node tag</param>
        /// <param name="type">Source type</param>
        /// <param name="val">Value collection</param>
        public PacketWriter PushList(string key, Type type, IEnumerable val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._ByteList(type, val);
            _ItemList()[key] = nod;
            return this;
        }

        /// <summary>
        /// Write key and collections
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

        internal void _Byte(MemoryStream str, ItemDictionary dic, int lvl)
        {
            if (lvl > _Level)
                throw new PacketException(PacketError.RecursiveError);
            foreach (var i in dic)
            {
                str._WriteExt(Encoding.UTF8.GetBytes(i.Key), true);
                if (i.Value is PacketWriter val)
                {
                    if (val._obj is null)
                    {
                        str._WriteLen(0);
                        continue;
                    }
                    if (val._obj is byte[] buf)
                    {
                        str._WriteExt(buf, true);
                        continue;
                    }
                }
                var pos = str.Position;
                str._WriteLen(0);
                if (i.Value is PacketWriter wtr)
                    _Byte(str, (ItemDictionary)wtr._obj, lvl + 1);
                else
                    ((PacketRawWriter)i.Value)._Write(str);
                var end = str.Position;
                str.Seek(pos, SeekOrigin.Begin);
                str._WriteLen((int)(end - pos - sizeof(int)));
                str.Seek(end, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Get binary packet
        /// </summary>
        public byte[] GetBytes()
        {
            if (_obj is byte[] buf)
                return buf;
            var dic = _obj as ItemDictionary;
            if (dic == null)
                return new byte[0];
            var mst = new MemoryStream();
            _Byte(mst, dic, 0);
            return mst.ToArray();
        }

        /// <summary>
        /// Create dynamic writer
        /// </summary>
        public DynamicMetaObject GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        /// <summary>
        /// Show byte count or node count
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

        internal static bool _ItemNode(object val, IReadOnlyDictionary<Type, IPacketConverter> cons, out object value)
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
                wtr = new PacketWriter(cons) { _obj = con.GetBytes(val) };
            else if (val.GetType()._IsEnumerable(out var inn))
                wtr = new PacketWriter(cons)._ByteList(inn, (IEnumerable)val);
            else
                wtr = null;
            value = wtr;
            return wtr != null;
        }

        internal static PacketWriter _Serialize(object val, IReadOnlyDictionary<Type, IPacketConverter> con, int lvl)
        {
            if (lvl > _Level)
                throw new PacketException(PacketError.RecursiveError);
            var wtr = new PacketWriter(con);

            void _push(string key, object obj)
            {
                var sub = _Serialize(obj, con, lvl + 1);
                wtr._ItemList()[key] = sub;
            }

            if (val is IDictionary<string, object> dic)
                foreach (var p in dic)
                    _push(p.Key, p.Value);
            else if (_ItemNode(val, con, out var nod))
                return (PacketWriter)nod;
            else
                foreach (var p in val.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    _push(p.Name, p.GetValue(val));
            return wtr;
        }

        /// <summary>
        /// Create new writer from object or dictionary (generic dictionary with string as key)
        /// </summary>
        public static PacketWriter Serialize(object obj, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            return _Serialize(obj, converters, 0);
        }
    }
}
