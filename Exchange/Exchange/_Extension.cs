using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Extension
    {
        internal static readonly byte[] s_zero_bytes = new byte[sizeof(int)];

        internal static readonly byte[] s_empty_bytes = new byte[0];

        internal static readonly char[] s_separators = new[] { '/', '\\' };

        internal static bool _HasNext(this byte[] buffer, int higher, ref int offset, out int length)
        {
            if (offset < 0 || higher - offset < sizeof(int))
                goto fail;
            length = BitConverter.ToInt32(buffer, offset);
            offset += sizeof(int);
            if (length < 0 || higher - offset < length)
                goto fail;
            return true;

            fail:
            length = 0;
            return false;
        }

        internal static void _Write(this Stream str, byte[] buf) => str.Write(buf, 0, buf.Length);
        
        internal static void _WriteKey(this Stream str, string key)
        {
            var buf = Encoding.UTF8.GetBytes(key);
            var len = BitConverter.GetBytes(buf.Length);
            str.Write(len, 0, len.Length);
            str.Write(buf, 0, buf.Length);
        }

        internal static void _WriteExt(this Stream str, byte[] buf)
        {
            var len = BitConverter.GetBytes(buf.Length);
            str.Write(len, 0, len.Length);
            str.Write(buf, 0, buf.Length);
        }

        internal static void _WriteExt(this Stream str, MemoryStream mst)
        {
            var len = (int)mst.Length;
            var buf = BitConverter.GetBytes(len);
            str.Write(buf, 0, sizeof(int));
            mst.WriteTo(str);
        }

        internal static byte[] _Span(byte[] buffer, int offset, int length)
        {
            if (offset == 0 && length == buffer.Length)
                return buffer;
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new PacketException(PacketError.Overflow);
            if (length == 0)
                return s_empty_bytes;
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
            var buf = BitConverter.GetBytes((int)len);
            str.Write(buf, 0, buf.Length);
            str.Position = dst;
        }

        internal static void _WriteValue(this Stream str, ConverterDictionary cvt, object itm, Type type)
        {
            var con = _Caches.GetConverter(cvt, type, false);
            var len = con.Length > 0;
            if (len)
                str._Write(con._GetBytesWrapError(itm));
            else
                str._WriteExt(con._GetBytesWrapError(itm));
            return;
        }

        internal static void _WriteValueGeneric<T>(this Stream str, ConverterDictionary cvt, T itm)
        {
            var con = _Caches.GetConverter<T>(cvt, false);
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

        internal static byte[] _ToBytes(this ICollection<byte> buffer)
        {
            var len = buffer?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            buffer.CopyTo(buf, 0);
            return buf;
        }

        internal static byte[] _ToBytes(this ICollection<sbyte> buffer)
        {
            var len = buffer?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            var tmp = new sbyte[len];
            buffer.CopyTo(tmp, 0);
            Buffer.BlockCopy(tmp, 0, buf, 0, len);
            return buf;
        }
    }
}
