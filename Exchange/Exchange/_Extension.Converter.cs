using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static System.BitConverter;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal static readonly Dictionary<Type, IPacketConverter> s_cons = null;

        internal static Dictionary<Type, IPacketConverter> _InitConverter()
        {
            var dic = new Dictionary<Type, IPacketConverter>();
            void _Ref<T>(Func<T, byte[]> bin, Func<byte[], int, int, T> val) => dic.Add(typeof(T), new _ConvertReference<T>(bin, val));
            void _Val<T>(Func<T, byte[]> bin, Func<byte[], int, T> val, int len) => dic.Add(typeof(T), new _ConvertValue<T>(bin, val, len));

            _Val(GetBytes, ToBoolean, sizeof(bool));
            _Val(_OfByte, _ToByte, sizeof(byte));
            _Val(_OfSByte, _ToSByte, sizeof(sbyte));
            _Val(GetBytes, ToChar, sizeof(char));
            _Val(GetBytes, ToInt16, sizeof(short));
            _Val(GetBytes, ToInt32, sizeof(int));
            _Val(GetBytes, ToInt64, sizeof(long));
            _Val(GetBytes, ToUInt16, sizeof(ushort));
            _Val(GetBytes, ToUInt32, sizeof(uint));
            _Val(GetBytes, ToUInt64, sizeof(ulong));
            _Val(GetBytes, ToSingle, sizeof(float));
            _Val(GetBytes, ToDouble, sizeof(double));
            _Val(_OfDecimal, _ToDecimal, sizeof(decimal));

            _Val(_OfDateTime, _ToDateTime, sizeof(long));
            _Val(_OfTimeSpan, _ToTimeSpan, sizeof(long));
            _Val(_OfGuid, _ToGuid, _GuidLength);

            _Ref(_OfBytes, _ToBytes);
            _Ref(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);
            _Ref(_OfIPAddress, _ToIPAddress);
            _Ref(_OfEndPoint, _ToEndPoint);

            return dic;
        }

        internal static void _Raise(Exception ex)
        {
            if (ex is PacketException || ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
                return;
            throw new PacketException(PacketError.ConvertError, ex);
        }

        internal static void _Overflow() => throw new PacketException(PacketError.Overflow);

        internal static object _GetValueWrapErr(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    _Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }

        internal static T _GetValueWrapErr<T>(this IPacketConverter con, byte[] buf, int off, int len, bool check)
        {
            try
            {
                if (check && con.Length > len)
                    _Overflow();
                if (con is IPacketConverter<T> res)
                    return res.GetValue(buf, off, len);
                return (T)con.GetValue(buf, off, len);
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }

        internal static byte[] _GetBytesWrapErr(this IPacketConverter con, object val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (con.Length > 0 && (buf == null || con.Length != buf.Length))
                    _Overflow();
                return buf;
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }

        internal static byte[] _GetBytesWrapErr<T>(this IPacketConverter<T> con, T val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (con.Length > 0 && (buf == null || con.Length != buf.Length))
                    _Overflow();
                return buf;
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }
    }
}
