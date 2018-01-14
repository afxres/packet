using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using WriterDirectory = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal const int _Length = 256;

        internal readonly ConverterDictionary _cvt = null;
        internal object _itm = null;

        public PacketWriter(ConverterDictionary converters = null) => _cvt = converters;

        internal WriterDirectory _GetItems()
        {
            if (_itm is WriterDirectory dic)
                return dic;
            var val = new WriterDirectory();
            _itm = val;
            return val;
        }

        public byte[] GetBytes()
        {
            if (_itm == null)
                return _Extension.s_empty_bytes;
            else if (_itm is byte[] buf)
                return buf;
            else if (_itm is PacketRawWriter raw)
                return raw._str.ToArray();
            var dic = (WriterDirectory)_itm;
            var mst = new MemoryStream(_Length);
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
                stb.AppendFormat("{0} node(s)", ((WriterDirectory)_itm).Count);
            return stb.ToString();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicWriter(parameter, this);

        internal static void _GetBytes(Stream str, WriterDirectory dic, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            foreach (var i in dic)
            {
                var key = i.Key;
                var obj = i.Value._itm;
                str._WriteKey(key);

                if (obj == null)
                    str._WriteLen(0);
                else if (obj is byte[] buf)
                    str._WriteExt(buf);
                else if (obj is PacketRawWriter raw)
                    str._WriteExt(raw._str);
                else
                {
                    var sub = (WriterDirectory)obj;
                    str._BeginInternal(out var src);
                    _GetBytes(str, sub, lev);
                    str._EndInternal(src);
                }
            }
        }

        internal static bool _GetWriter(object itm, ConverterDictionary cvt, out PacketWriter val)
        {
            var obj = default(object);
            var con = default(IPacketConverter);
            var typ = default(Type);
            var buf = default(byte[]);
            var det = default(_DetailInfo);

            if ((typ = itm?.GetType()) == null)
                obj = null;
            else if (itm is PacketWriter wri)
                obj = wri._itm;
            else if (itm is PacketRawWriter raw)
                obj = raw;
            else if ((con = _Caches.GetConverter(cvt, typ, true)) != null)
                obj = con._GetBytesWrapError(itm);
            else if ((det = _Caches.GetDetail(typ)).is_itr_imp == false)
                goto fail;
            else if (det.arg_of_itr_imp == typeof(byte) && itm is ICollection<byte> byt)
                obj = byt._ToBytes();
            else if (det.arg_of_itr_imp == typeof(sbyte) && itm is ICollection<sbyte> sby)
                obj = sby._ToBytes();
            else if ((buf = _Caches.GetBytesEnumerableReflection(cvt, (IEnumerable)itm, det.arg_of_itr_imp)) != null)
                obj = buf;
            else goto fail;

            val = new PacketWriter(cvt) { _itm = obj };
            return true;

            fail:
            val = null;
            return false;
        }

        internal static PacketWriter _Serialize(ConverterDictionary cvt, object itm, int lev)
        {
            if (lev > _Caches._Depth)
                throw new PacketException(PacketError.RecursiveError);
            if (_GetWriter(itm, cvt, out var sub))
                return sub;
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

        internal static void _SerializeProperties(WriterDirectory dst, ConverterDictionary cvt, object itm, int lev)
        {
            var typ = itm.GetType();
            var inf = _Caches.GetGetMethods(typ);
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
