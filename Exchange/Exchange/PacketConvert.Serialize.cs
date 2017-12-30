using System.Collections;
using System.Collections.Generic;
using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        internal static void _SerializeDictionary(Stream str, ConverterDictionary cvt, IDictionary<string, object> dic, int lev)
        {
            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                str._WriteKey(key);
                str._BeginInternal(out var src);
                _Serialize(str, cvt, val, lev);
                str._EndInternal(src);
            }
        }

        internal static void _SerializeProperties(Stream str, ConverterDictionary cvt, object itm, int lev)
        {
            var typ = itm.GetType();
            var inf = _Caches.GetMethods(typ);
            var fun = inf.func;
            var arg = inf.args;
            var res = new object[arg.Length];
            fun.Invoke(itm, res);
            for (int i = 0; i < arg.Length; i++)
            {
                var key = arg[i].name;
                var val = res[i];
                str._WriteKey(key);
                str._BeginInternal(out var src);
                _Serialize(str, cvt, val, lev);
                str._EndInternal(src);
            }
        }

        internal static void _Serialize(Stream str, ConverterDictionary cvt, object itm, int lev)
        {
            if (itm == null)
                return;
            var typ = itm.GetType();
            var con = _Caches.Converter(cvt, typ, true);
            if (con != null)
                str._Write(con._GetBytesWrapError(itm));
            else
                _SerializeComplex(str, cvt, itm, lev);
            return;
        }


        internal static void _SerializeComplex(Stream str, ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var typ = itm.GetType();
            var con = default(IPacketConverter);
            if (itm is IEnumerable && typ._IsImplOfEnumerable(out var inn) && (con = _Caches.Converter(cvt, inn, true)) != null)
                str._WriteEnumerable(con, (IEnumerable)itm);
            else if (itm is IDictionary<string, object> dic)
                _SerializeDictionary(str, cvt, dic, lev);
            else
                _SerializeProperties(str, cvt, itm, lev);
            return;
        }

        public static byte[] Serialize(object value, ConverterDictionary converters = null)
        {
            var itm = value;
            if (itm == null)
                return new byte[0];
            var typ = itm.GetType();
            if (itm is byte[] buf)
                return buf;

            var cvt = converters;
            var con = _Caches.Converter(cvt, typ, true);
            if (con != null)
                return con._GetBytesWrapError(itm);

            var str = _Caches.GetStream();
            _SerializeComplex(str, cvt, itm, 0);
            return str.ToArray();
        }

        public static byte[] Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null)
        {
            return Serialize((object)dictionary, converters);
        }
    }
}
