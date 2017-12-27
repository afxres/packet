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
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal object _obj = null;
        internal readonly ConverterDictionary _con = null;

        public PacketWriter(ConverterDictionary converters = null) => _con = converters;

        internal ItemDirectory _GetItems()
        {
            if (_obj is ItemDirectory dic)
                return dic;
            var val = new ItemDirectory();
            _obj = val;
            return val;
        }

        [Obsolete]
        public PacketWriter Push(string key, PacketWriter val)
        {
            _GetItems()[key] = new PacketWriter(_con) { _obj = val?._obj };
            return this;
        }

        [Obsolete]
        public PacketWriter Push(string key, PacketRawWriter val)
        {
            _GetItems()[key] = val;
            return this;
        }

        [Obsolete]
        public PacketWriter Push(string key, Type type, object val)
        {
            var buf = _Caches.GetBytes(type, _con, val);
            _GetItems()[key] = new PacketWriter(_con) { _obj = buf };
            return this;
        }

        [Obsolete]
        public PacketWriter Push<T>(string key, T val)
        {
            var buf = _Caches.GetBytes(_con, val);
            _GetItems()[key] = new PacketWriter(_con) { _obj = buf };
            return this;
        }

        [Obsolete]
        public PacketWriter PushList(string key, byte[] buf)
        {
            var nod = new PacketWriter(_con) { _obj = buf };
            _GetItems()[key] = nod;
            return this;
        }

        internal void _GetBytes(Type type, IEnumerable val)
        {
            var con = _Caches.Converter(type, _con, false);
            _GetBytes(con, val);
        }

        internal PacketWriter _GetBytes(IPacketConverter con, IEnumerable val)
        {
            var hea = con.Length < 1;
            var mst = _GetStream();
            foreach (var i in val)
                mst._WriteOpt(con._GetBytesWrapError(i), hea);
            _obj = mst.ToArray();
            return this;
        }

        internal void _GetBytes<T>(IEnumerable<T> val)
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var hea = con.Length < 1;
            var mst = _GetStream();
            if (con is IPacketConverter<T> res)
                foreach (var i in val)
                    mst._WriteOpt(res._GetBytesWrapError(i), hea);
            else
                foreach (var i in val)
                    mst._WriteOpt(con._GetBytesWrapError(i), hea);
            _obj = mst.ToArray();
        }

        [Obsolete]
        public PacketWriter PushList(string key, Type type, IEnumerable val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._GetBytes(type, val);
            _GetItems()[key] = nod;
            return this;
        }

        [Obsolete]
        public PacketWriter PushList<T>(string key, IEnumerable<T> val)
        {
            var nod = new PacketWriter(_con);
            if (val != null)
                nod._GetBytes(val);
            _GetItems()[key] = nod;
            return this;
        }

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
                wtr = new PacketWriter(cons) { _obj = con._GetBytesWrapError(val) };
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
                _SerializeViaGetMethods(lst, val, cons, lev);
            return wtr;
        }

        internal static void _SerializeViaGetMethods(ItemDirectory items, object val, ConverterDictionary cons, int lev)
        {
            var inf = _Caches.GetMethods(val.GetType());
            var fun = inf.func;
            var arg = inf.args;
            var res = new object[arg.Length];
            fun.Invoke(val, res);
            for (int i = 0; i < arg.Length; i++)
                items[arg[i].name] = _Serialize(res[i], cons, lev + 1);
            return;
        }

        public static PacketWriter Serialize(object obj, ConverterDictionary converters = null) => _Serialize(obj, converters, 0);
    }
}
