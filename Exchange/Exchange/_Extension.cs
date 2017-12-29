using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static System.BitConverter;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Extension
    {
        internal static readonly char[] s_seps = new[] { '/', '\\' };

        static _Extension()
        {
            s_cons = _InitDictionary();
        }

        internal static bool _IsEnumerable(this Type type, out Type inner)
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

        internal static bool _IsImplOfEnumerable(this Type type, out Type inner)
        {
            foreach (var i in type.GetInterfaces())
                if (i._IsEnumerable(out inner))
                    return true;
            inner = null;
            return false;
        }

        internal static bool _IsArray(this Type type, out Type inner)
        {
            if (type.IsArray == false || type.GetArrayRank() != 1)
                goto fail;
            inner = type.GetElementType();
            return true;

            fail:
            inner = null;
            return false;
        }

        /// <summary>
        /// Is <see cref="List{T}"/> or <see cref="IList{T}"/>
        /// </summary>
        internal static bool _IsList(this Type type, out Type inner)
        {
            if (type.IsGenericType == false)
                goto fail;
            var def = type.GetGenericTypeDefinition();
            if (def != typeof(List<>) && def != typeof(IList<>))
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

        internal static bool _CanRead(this byte[] buffer, int higher, ref int offset, out int length)
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

        internal static void _WriteExt(this Stream stream, byte[] buffer)
        {
            stream._Write(GetBytes(buffer.Length));
            stream._Write(buffer);
        }

        internal static void _WriteExt(this Stream str, MemoryStream mst)
        {
            var len = mst.Length;
            if (len > int.MaxValue)
                throw new PacketException(PacketError.Overflow);
            str._WriteLen((int)len);
            mst.WriteTo(str);
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

        internal static void _BeginInternal(this Stream str, out long src)
        {
            var pos = str.Position;
            str.Position += sizeof(int);
            src = pos;
        }

        internal static void _EndInternal(this Stream str, long src)
        {
            var dst = str.Position;
            var len = dst - src - sizeof(int);
            if (len > int.MaxValue)
                throw new PacketException(PacketError.Overflow);
            str.Position = src;
            var buf = GetBytes((int)len);
            str.Write(buf, 0, buf.Length);
            str.Position = dst;
        }


        internal static void _WriteValue(this Stream str, ConverterDictionary cvt, object itm, Type type)
        {
            var con = _Caches.Converter(cvt, type, false);
            var len = con.Length > 0;
            if (len)
                str._Write(con._GetBytesWrapError(itm));
            else
                str._WriteExt(con._GetBytesWrapError(itm));
            return;
        }

        internal static void _WriteValueGeneric<T>(this Stream str, ConverterDictionary cvt, T itm)
        {
            var con = _Caches.Converter<T>(cvt, false);
            var len = con.Length > 0;
            var res = con as IPacketConverter<T>;
            if (len && res != null)
                str._Write(res._GetBytesWrapErrorGeneric(itm));
            else if (len)
                str._Write(con._GetBytesWrapError(itm));
            else if (res != null)
                str._WriteExt(res._GetBytesWrapErrorGeneric(itm));
            else
                str._WriteExt(con._GetBytesWrapError(itm));
            return;
        }

        internal static void _WriteEnumerable(this Stream str, IPacketConverter con, IEnumerable itr)
        {
            var len = con.Length > 0;
            if (len)
                foreach (var i in itr)
                    str._Write(con._GetBytesWrapError(i));
            else
                foreach (var i in itr)
                    str._WriteExt(con._GetBytesWrapError(i));
            return;
        }

        internal static void _WriteEnumerableGeneric<T>(this Stream str, IPacketConverter con, IEnumerable<T> itr)
        {
            var len = con.Length > 0;
            var res = con as IPacketConverter<T>;

            if (len && res != null)
                foreach (var i in itr)
                    str._Write(res._GetBytesWrapErrorGeneric(i));
            else if (len)
                foreach (var i in itr)
                    str._Write(con._GetBytesWrapError(i));
            else if (res != null)
                foreach (var i in itr)
                    str._WriteExt(res._GetBytesWrapErrorGeneric(i));
            else
                foreach (var i in itr)
                    str._WriteExt(con._GetBytesWrapError(i));
            return;
        }
    }
}
