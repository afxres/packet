using System;
using System.Text;
using System.Threading;
using static System.BitConverter;
using TypeTools = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal static readonly TypeTools s_cons = null;

        internal static void _Ref<T>(TypeTools dic, Func<T, byte[]> bin, Func<byte[], int, int, T> val) => dic.Add(typeof(T), new _ConvertReference<T>(bin, val));

        internal static void _Val<T>(TypeTools dic, Func<T, byte[]> bin, Func<byte[], int, T> val, int len) => dic.Add(typeof(T), new _ConvertValue<T>(bin, val, len));

        internal static TypeTools _InitConverter()
        {
            var dic = new TypeTools();

            _Val(dic, GetBytes, ToBoolean, sizeof(bool));
            _Val(dic, _OfByte, _ToByte, sizeof(byte));
            _Val(dic, _OfSByte, _ToSByte, sizeof(sbyte));
            _Val(dic, GetBytes, ToChar, sizeof(char));
            _Val(dic, GetBytes, ToInt16, sizeof(short));
            _Val(dic, GetBytes, ToInt32, sizeof(int));
            _Val(dic, GetBytes, ToInt64, sizeof(long));
            _Val(dic, GetBytes, ToUInt16, sizeof(ushort));
            _Val(dic, GetBytes, ToUInt32, sizeof(uint));
            _Val(dic, GetBytes, ToUInt64, sizeof(ulong));
            _Val(dic, GetBytes, ToSingle, sizeof(float));
            _Val(dic, GetBytes, ToDouble, sizeof(double));
            _Val(dic, _OfDecimal, _ToDecimal, sizeof(decimal));

            _Val(dic, _OfDateTime, _ToDateTime, sizeof(long));
            _Val(dic, _OfTimeSpan, _ToTimeSpan, sizeof(long));
            _Val(dic, _OfGuid, _ToGuid, _GuidLength);

            _Ref(dic, _OfBytes, _ToBytes);
            _Ref(dic, Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);
            _Ref(dic, _OfIPAddress, _ToIPAddress);
            _Ref(dic, _OfEndPoint, _ToEndPoint);

            return dic;
        }

        internal static bool _WrapErr(Exception ex) => (ex is PacketException || ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException) == false;

        internal static Exception _Overflow() => new PacketException(PacketError.Overflow);

        internal static Exception _Raise(Exception ex) => new PacketException(PacketError.ConvertError, ex);

        internal static object _GetValueWrapErr(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapErr(ex))
            {
                throw _Raise(ex);
            }
        }

        internal static T _GetValueWrapErr<T>(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    throw _Overflow();
                if (con is IPacketConverter<T> res)
                    return res.GetValue(buf, off, len);
                return (T)con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (_WrapErr(ex))
            {
                throw _Raise(ex);
            }
        }

        internal static byte[] _GetBytesWrapErr(this IPacketConverter con, object val)
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
            catch (Exception ex) when (_WrapErr(ex))
            {
                throw _Raise(ex);
            }
        }

        internal static byte[] _GetBytesWrapErr<T>(this IPacketConverter<T> con, T val)
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
            catch (Exception ex) when (_WrapErr(ex))
            {
                throw _Raise(ex);
            }
        }
    }
}
