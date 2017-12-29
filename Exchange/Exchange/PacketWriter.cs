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

        internal static void _GetBytes(MemoryStream str, ItemDirectory dic, int level)
        {
            if (level > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                str._WriteExt(Encoding.UTF8.GetBytes(key));

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
                    _GetBytes(str, nod, level);
                    str._EndInternal(src);
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
                wtr = new PacketWriter(cons) { _itm = wri._itm };
            else if ((con = _Caches.Converter(cons, val.GetType(), true)) != null)
                wtr = new PacketWriter(cons) { _itm = con._GetBytesWrapError(val) };
            else if (val.GetType()._IsImplOfEnumerable(out var inn) && (con = _Caches.Converter(cons, inn, true)) != null)
                wtr = new PacketWriter(cons) { _itm = _Caches.GetBytes(con, (IEnumerable)val) };
            else goto fail;

            value = wtr;
            return true;

            fail:
            value = null;
            return false;
        }

        internal static PacketWriter _Serialize(int lev, ConverterDictionary cvt, object itm)
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
                    lst[i.Key] = _Serialize(lev, cvt, i.Value);
            else
                _SerializeViaGetMethods(lev, lst, cvt, itm);
            return wtr;
        }

        internal static void _SerializeViaGetMethods(int lev, ItemDirectory dst, ConverterDictionary cvt, object itm)
        {
            var typ = itm.GetType();
            var inf = _Caches.GetMethods(typ);
            var fun = inf.func;
            var arg = inf.args;
            var res = new object[arg.Length];
            fun.Invoke(itm, res);
            for (int i = 0; i < arg.Length; i++)
                dst[arg[i].name] = _Serialize(lev, cvt, res[i]);
            return;
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => _Serialize(0, converters, value);

        public static PacketWriter Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null) => Serialize((object)dictionary, converters);
    }
}
