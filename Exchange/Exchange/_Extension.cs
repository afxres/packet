using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static System.BitConverter;

namespace Mikodev.Network
{
    /// <summary>
    /// Extend functions
    /// </summary>
    public static partial class _Extension
    {
        internal static readonly char[] s_seps = new[] { '/', '\\' };

        internal static readonly Dictionary<Type, IPacketConverter> s_cons = null;

        static _Extension()
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

            s_cons = dic;
        }

        internal static bool _IsEnumerableGeneric(this Type type, out Type inner)
        {
            if (type.IsGenericType == false || type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                goto fail;
            var som = type.GetGenericArguments();
            if (som.Length != 1)
                goto fail;
            inner = som[0];
            return true;
            fail:
            inner = null;
            return false;
        }

        internal static bool _IsEnumerable(this Type typ, out Type inn)
        {
            foreach (var i in typ.GetInterfaces())
                if (i._IsEnumerableGeneric(out inn))
                    return true;
            inn = null;
            return false;
        }

        /// <summary>
        /// Read length from buffer, return false if out if range
        /// </summary>
        internal static bool _Read(this byte[] buffer, int higher, ref int offset, out int length)
        {
            if (offset < 0 || higher - offset < sizeof(int))
                goto fail;
            length = ToInt32(buffer, offset);
            offset += sizeof(int);
            if (length < 0 || higher - offset < length)
                goto fail;
            return true;

            fail:
            length = 0;
            return false;
        }

        internal static void _Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void _WriteLen(this Stream stream, int value) => stream._Write(GetBytes(value));

        internal static void _WriteExt(this Stream stream, byte[] buffer, bool header)
        {
            if (header)
                stream._Write(GetBytes(buffer.Length));
            stream._Write(buffer);
        }

        internal static byte[] _Borrow(byte[] buffer, int offset, int length)
        {
            if (offset == 0 && length == buffer.Length)
                return buffer;
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new PacketException(PacketError.Overflow);
            var buf = new byte[length];
            Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        internal static T _GetValue<T>(this IPacketConverter con, byte[] buffer, int offset, int length)
        {
            if (con is IPacketConverter<T> res)
                return res.GetValue(buffer, offset, length);
            return (T)con.GetValue(buffer, offset, length);
        }
    }
}
