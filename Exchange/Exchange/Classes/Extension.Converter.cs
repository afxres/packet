using Mikodev.Network.Converters;
using System;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    partial class Extension
    {
        internal static readonly ConverterDictionary s_converters = null;

        static Extension()
        {
            var dic = new ConverterDictionary();
            var ass = typeof(Extension).Assembly;

            foreach (var t in ass.GetTypes())
            {
                var ats = t.GetCustomAttributes(typeof(PacketConverterAttribute), false);
                if (ats.Length != 1)
                    continue;
                var att = (PacketConverterAttribute)ats[0];
                var typ = att.Type;
                var ins = (IPacketConverter)Activator.CreateInstance(t);
                dic.Add(typ, ins);
            }
            s_converters = dic;
        }

        internal static object GetValueWrap(this IPacketConverter con, Element ele, bool check = false)
        {
            return GetValueWrap(con, ele.buffer, ele.offset, ele.length, check);
        }

        internal static object GetValueWrap(this IPacketConverter con, byte[] buf, int off, int len, bool check = false)
        {
            try
            {
                if (check && con.Length > len)
                    throw PacketException.Overflow();
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

        internal static T GetValueWrapAuto<T>(this IPacketConverter con, Element element, bool check = false)
        {
            return GetValueWrapAuto<T>(con, element.buffer, element.offset, element.length, check);
        }

        internal static T GetValueWrapAuto<T>(this IPacketConverter con, byte[] buf, int off, int len, bool check = false)
        {
            try
            {
                if (check && con.Length > len)
                    throw PacketException.Overflow();
                if (con is IPacketConverter<T> res)
                    return res.GetValue(buf, off, len);
                return (T)con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

        internal static T GetValue<T>(this IPacketConverter<T> con, Element ele)
        {
            return con.GetValue(ele.buffer, ele.offset, ele.length);
        }

        internal static object GetValue(this IPacketConverter con, Element ele)
        {
            return con.GetValue(ele.buffer, ele.offset, ele.length);
        }

        internal static T GetValueWrap<T>(this IPacketConverter<T> con, byte[] buf, int off, int len)
        {
            try
            {
                return con.GetValue(buf, off, len);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

        internal static T GetValueWrap<T>(this IPacketConverter<T> con, Element ele)
        {
            try
            {
                return con.GetValue(ele.buffer, ele.offset, ele.length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

        internal static byte[] GetBytesWrap(this IPacketConverter con, object val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = con.Length;
                if (len > 0 && len != buf.Length)
                    throw PacketException.ConvertMismatch(len);
                return buf;
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

        internal static byte[] GetBytesWrap<T>(this IPacketConverter<T> con, T val)
        {
            try
            {
                var buf = con.GetBytes(val);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = con.Length;
                if (len > 0 && len != buf.Length)
                    throw PacketException.ConvertMismatch(len);
                return buf;
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }
    }
}
