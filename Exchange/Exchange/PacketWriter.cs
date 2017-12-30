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
        internal object _itm = null;
        internal readonly ConverterDictionary _cvt = null;

        public PacketWriter(ConverterDictionary converters = null) => _cvt = converters;

        internal ItemDirectory _GetItems()
        {
            if (_itm is ItemDirectory dic)
                return dic;
            var val = new ItemDirectory();
            _itm = val;
            return val;
        }

        public byte[] GetBytes()
        {
            if (_itm is byte[] buf)
                return buf;
            var dic = _itm as ItemDirectory;
            if (dic == null)
                return new byte[0];
            var mst = _Caches.GetStream();
            _GetBytes(mst, dic, 0);
            var res = mst.ToArray();
            return res;
        }

        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketWriter));
            stb.Append(" with ");
            if (_itm is null)
                stb.Append("none");
            else if (_itm is byte[] buf)
                stb.AppendFormat("{0} byte(s)", buf.Length);
            else
                stb.AppendFormat("{0} node(s)", ((ItemDirectory)_itm).Count);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void _GetBytes(Stream str, ItemDirectory dic, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                str._WriteKey(key);

                if (val is PacketRawWriter raw)
                {
                    str._WriteExt(raw._str);
                    continue;
                }
                var wtr = (PacketWriter)val;
                var obj = wtr._itm;

                if (obj == null)
                    str._WriteLen(0);
                else if (obj is byte[] buf)
                    str._WriteExt(buf);
                else
                {
                    var nod = (ItemDirectory)obj;
                    str._BeginInternal(out var src);
                    _GetBytes(str, nod, lev);
                    str._EndInternal(src);
                }
            }
        }

        internal static bool _GetWriter(object itm, ConverterDictionary cvt, out object val)
        {
            var wtr = default(object);
            var con = default(IPacketConverter);
            var typ = default(Type);

            if ((typ = itm?.GetType()) == null)
                wtr = new PacketWriter(cvt);
            else if (itm is PacketRawWriter raw)
                wtr = raw;
            else if (itm is PacketWriter wri)
                wtr = new PacketWriter(cvt) { _itm = wri._itm };
            else if ((con = _Caches.Converter(cvt, typ, true)) != null)
                wtr = new PacketWriter(cvt) { _itm = con._GetBytesWrapError(itm) };
            else if (itm is IEnumerable && typ._IsImplOfEnumerable(out var inn) && (con = _Caches.Converter(cvt, inn, true)) != null)
                wtr = new PacketWriter(cvt) { _itm = _Caches.GetBytes(con, (IEnumerable)itm) };
            else goto fail;

            val = wtr;
            return true;

            fail:
            val = null;
            return false;
        }

        internal static PacketWriter _Serialize(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            if (_GetWriter(itm, cvt, out var nod))
                return (PacketWriter)nod;
            lev += 1;

            var wtr = new PacketWriter(cvt);
            var lst = wtr._GetItems();

            if (itm is IDictionary<string, object> dic)
                foreach (var i in dic)
                    lst[i.Key] = _Serialize(cvt, i.Value, lev);
            else
                _SerializeProperties(lst, cvt, itm, lev);
            return wtr;
        }

        internal static void _SerializeProperties(ItemDirectory dst, ConverterDictionary cvt, object itm, int lev)
        {
            var typ = itm.GetType();
            var inf = _Caches.GetMethods(typ);
            var fun = inf.func;
            var arg = inf.args;
            var res = new object[arg.Length];
            fun.Invoke(itm, res);
            for (int i = 0; i < arg.Length; i++)
                dst[arg[i].name] = _Serialize(cvt, res[i], lev);
            return;
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => _Serialize(converters, value, 0);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
