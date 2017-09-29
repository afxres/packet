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
    public sealed class PacketWriter : IDynamicMetaObjectProvider
    {
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
            nod._obj = con._GetBytesWrapErr(val);
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
                nod._obj = res._GetBytesWrapErr(val);
            else
                nod._obj = con._GetBytesWrapErr(val);
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

        internal PacketWriter _ByteList(Type type, IEnumerable val)
        {
            var con = _Caches.Converter(type, _con, false);
            var hea = con.Length < 1;
            var mst = new MemoryStream(_Caches._StrInit);
            foreach (var i in val)
                mst._WriteExt(con._GetBytesWrapErr(i), hea);
            _obj = mst.ToArray();
            return this;
        }

        internal PacketWriter _ByteList<T>(IEnumerable<T> val)
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var hea = con.Length < 1;
            var mst = new MemoryStream(_Caches._StrInit);
            if (con is IPacketConverter<T> res)
                foreach (var i in val)
                    mst._WriteExt(res._GetBytesWrapErr(i), hea);
            else
                foreach (var i in val)
                    mst._WriteExt(con._GetBytesWrapErr(i), hea);
            _obj = mst.ToArray();
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
            var mst = new MemoryStream(_Caches._StrInit);
            _Byte(mst, dic, 0);
            return mst.ToArray();
        }

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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void _Byte(Stream str, ItemDictionary dic, int lvl)
        {
            if (lvl > _Caches._RecDeep)
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
                    ((PacketRawWriter)i.Value)._mst.WriteTo(str);
                var end = str.Position;
                str.Position = pos;
                str._WriteLen((int)(end - pos - sizeof(int)));
                str.Position = end;
            }
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
                wtr = new PacketWriter(cons) { _obj = con._GetBytesWrapErr(val) };
            else if (val.GetType()._IsEnumerable(out var inn))
                wtr = new PacketWriter(cons)._ByteList(inn, (IEnumerable)val);
            else
                wtr = null;
            value = wtr;
            return wtr != null;
        }

        internal static void _SerializePush(string key, object obj, PacketWriter wtr, IReadOnlyDictionary<Type, IPacketConverter> con, int lvl)
        {
            var sub = _Serialize(obj, con, lvl + 1);
            var lst = wtr._ItemList();
            lst[key] = sub;
        }

        internal static PacketWriter _Serialize(object val, IReadOnlyDictionary<Type, IPacketConverter> con, int lvl)
        {
            if (lvl > _Caches._RecDeep)
                throw new PacketException(PacketError.RecursiveError);
            var wtr = new PacketWriter(con);

            if (val is IDictionary<string, object> dic)
                foreach (var p in dic)
                    _SerializePush(p.Key, p.Value, wtr, con, lvl);
            else if (_ItemNode(val, con, out var nod))
                return (PacketWriter)nod;
            else
                foreach (var p in val.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    _SerializePush(p.Name, p.GetValue(val), wtr, con, lvl);
            return wtr;
        }

        /// <summary>
        /// Create new writer from object or dictionary (generic dictionary with string as key)
        /// </summary>
        public static PacketWriter Serialize(object obj, IReadOnlyDictionary<Type, IPacketConverter> converters = null) => _Serialize(obj, converters, 0);
    }
}
