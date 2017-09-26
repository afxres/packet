using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using static System.BitConverter;

namespace Mikodev.Network
{
    /// <summary>
    /// Extend functions
    /// </summary>
    public static partial class _Extension
    {
        internal static readonly string[] s_seps = new string[] { "/", @"\" };

        internal static readonly Dictionary<Type, IPacketConverter> s_cons = new Dictionary<Type, IPacketConverter>();

        static _Extension()
        {
            _Emit(GetBytes, ToBoolean, sizeof(bool));
            _Emit(_OfByte, _ToByte, sizeof(byte));
            _Emit(_OfSByte, _ToSByte, sizeof(sbyte));
            _Emit(GetBytes, ToChar, sizeof(char));
            _Emit(GetBytes, ToInt16, sizeof(short));
            _Emit(GetBytes, ToInt32, sizeof(int));
            _Emit(GetBytes, ToInt64, sizeof(long));
            _Emit(GetBytes, ToUInt16, sizeof(ushort));
            _Emit(GetBytes, ToUInt32, sizeof(uint));
            _Emit(GetBytes, ToUInt64, sizeof(ulong));
            _Emit(GetBytes, ToSingle, sizeof(float));
            _Emit(GetBytes, ToDouble, sizeof(double));
            _Emit(_OfDecimal, _ToDecimal, sizeof(decimal));

            _Emit(_OfDateTime, _ToDateTime, sizeof(long));
            _Emit(_OfTimeSpan, _ToTimeSpan, sizeof(long));
            _Emit(_OfGuid, _ToGuid, _GuidLength);

            _Emit(_ToBytes, _OfBytes);
            _Emit(Encoding.UTF8.GetBytes, Encoding.UTF8.GetString);
            _Emit(_OfIPAddress, _ToIPAddress);
            _Emit(_OfEndPoint, _ToEndPoint);
        }

        internal static void _Emit<T>(Func<T, byte[]> bin, Func<byte[], int, int, T> val) => s_cons.Add(typeof(T), new _ConvRef<T>(bin, val));

        internal static void _Emit<T>(Func<T, byte[]> bin, Func<byte[], int, T> val, int len) => s_cons.Add(typeof(T), new _ConvVal<T>(bin, val, len));

        internal static bool _IsGenericEnumerable(this Type type, out Type inner)
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
                if (i._IsGenericEnumerable(out inn))
                    return true;
            inn = null;
            return false;
        }

        /// <summary>
        /// Read length from buffer, return false if out if range
        /// </summary>
        internal static bool _Read(this byte[] buffer, ref int offset, out int length, int higher = -1)
        {
            if (higher < 0)
                higher = buffer.Length;
            else if (buffer.Length < higher)
                throw new PacketException(PacketError.AssertFailed);

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

        internal static void _WriteExt(this Stream stream, byte[] buffer)
        {
            var len = GetBytes(buffer.Length);
            stream._Write(len);
            stream._Write(buffer);
        }

        internal static bool _Catch(Exception ex) => (ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException) == false;
    }
}
