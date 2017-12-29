using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        internal static void _SerializeDictionary(int lev, Stream str, ConverterDictionary cvt, IDictionary<string, object> dic)
        {
            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                var buf = Encoding.UTF8.GetBytes(key);
                str._WriteExt(buf);
                str._BeginInternal(out var src);
                _Serialize(lev, str, cvt, val);
                str._EndInternal(src);
            }
        }

        internal static void _SerializeViaGetMethods(int lev, Stream str, ConverterDictionary cvt, object itm)
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
                var buf = Encoding.UTF8.GetBytes(key);
                str._WriteExt(buf);
                str._BeginInternal(out var src);
                _Serialize(lev, str, cvt, val);
                str._EndInternal(src);
            }
        }


        internal static void _Serialize(int lev, Stream str, ConverterDictionary cvt, object itm)
        {
            if (itm == null)
                return;
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var typ = itm.GetType();
            var con = default(IPacketConverter);
            if ((con = _Caches.Converter(cvt, typ, true)) != null)
                str._Write(con._GetBytesWrapError(itm));
            else if (typ._IsImplOfEnumerable(out var inn) && (con = _Caches.Converter(cvt, inn, true)) != null)
                str._WriteEnumerable(con, (IEnumerable)itm);
            else if (itm is IDictionary<string, object> dic)
                _SerializeDictionary(lev, str, cvt, dic);
            else
                _SerializeViaGetMethods(lev, str, cvt, itm);
            return;
        }

        public static byte[] Serialize(object value, ConverterDictionary converters = null)
        {
            if (value == null)
                return new byte[0];
            if (value is byte[] buf)
                return buf;
            var str = _Caches.GetStream();
            _Serialize(0, str, converters, value);
            return str.ToArray();
        }

        public static byte[] Serialize(IDictionary<string, object> dictionary, ConverterDictionary converters = null)
        {
            return Serialize((object)dictionary, converters);
        }
    }
}
