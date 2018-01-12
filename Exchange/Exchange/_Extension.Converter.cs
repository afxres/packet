using Mikodev.Network.Converters;
using System;
using System.Text;
using System.Threading;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal static readonly ConverterDictionary s_cons = null;

        internal static void _Ref<T>(ConverterDictionary dic, Func<T, byte[]> bin, Func<byte[], int, int, T> val) => dic.Add(typeof(T), new _ConvertReference<T>(bin, val));

        internal static ConverterDictionary _InitDictionary()
        {
            var dic = new ConverterDictionary();

            var ass = typeof(_Extension).Assembly;
            var tps = ass.GetTypes();
            foreach (var t in ass.GetTypes())
            {
                var ats = t.GetCustomAttributes(typeof(_ConverterAttribute), false);
                if (ats.Length != 1)
                    continue;
                var att = (_ConverterAttribute)ats[0];
                var typ = att.Type;
                var ins = (IPacketConverter)Activator.CreateInstance(t);
                dic.Add(typ, ins);
            }

            _Ref(dic, _OfByteArray, _ToByteArray);
            _Ref(dic, _OfSByteArray, _ToSByteArray);
            _Ref(dic, Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);
            _Ref(dic, _OfIPAddress, _ToIPAddress);
            _Ref(dic, _OfEndPoint, _ToEndPoint);

            return dic;
        }

        internal static bool _WrapError(Exception ex) => (ex is PacketException || ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException) == false;

        internal static Exception _Overflow() => new PacketException(PacketError.Overflow);

        internal static Exception _ConvertError(Exception ex) => new PacketException(PacketError.ConvertError, ex);

        internal static object _GetValueWrapError(this IPacketConverter con, _Element element, bool check)
        {
            return _GetValueWrapError(con, element._buf, element._off, element._len, check);
        }

        internal static object _GetValueWrapError(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static T _GetValueWrapErrorAuto<T>(this IPacketConverter con, _Element element, bool check)
        {
            return _GetValueWrapErrorAuto<T>(con, element._buf, element._off, element._len, check);
        }

        internal static T _GetValueWrapErrorAuto<T>(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                if (con is IPacketConverter<T> res)
                    return res.GetValue(buf, off, len);
                return (T)con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static T _GetValueWrapErrorGeneric<T>(this IPacketConverter<T> con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static byte[] _GetBytesWrapError(this IPacketConverter con, object val)
        {
            try
            {
                var buf = con.GetBytes(val);
#if DEBUG
                if (con.Length > 0 && (buf == null || con.Length != buf.Length))
                    throw _Overflow();
#endif
                return buf;
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }

        internal static byte[] _GetBytesWrapErrorGeneric<T>(this IPacketConverter<T> con, T val)
        {
            try
            {
                var buf = con.GetBytes(val);
#if DEBUG
                if (con.Length > 0 && (buf == null || con.Length != buf.Length))
                    throw _Overflow();
#endif
                return buf;
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
        }
    }
}
