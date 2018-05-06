using Mikodev.Network.Converters;
using System;
using System.Runtime.CompilerServices;
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

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetValueWrap(this IPacketConverter converter, Element element, bool check = false)
        {
            return GetValueWrap(converter, element.buffer, element.offset, element.length, check);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetValueWrap(this IPacketConverter converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                return converter.GetValue(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrapAuto<T>(this IPacketConverter converter, Element element, bool check = false)
        {
            return GetValueWrapAuto<T>(converter, element.buffer, element.offset, element.length, check);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrapAuto<T>(this IPacketConverter converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                if (converter is IPacketConverter<T> res)
                    return res.GetValue(buffer, offset, length);
                return (T)converter.GetValue(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValue<T>(this IPacketConverter<T> converter, Element element)
        {
            return converter.GetValue(element.buffer, element.offset, element.length);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetValue(this IPacketConverter converter, Element element)
        {
            return converter.GetValue(element.buffer, element.offset, element.length);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrap<T>(this IPacketConverter<T> converter, byte[] buffer, int offset, int length)
        {
            try
            {
                return converter.GetValue(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrap<T>(this IPacketConverter<T> converter, Element element)
        {
            try
            {
                return converter.GetValue(element.buffer, element.offset, element.length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static byte[] GetBytesWrap(this IPacketConverter converter, object value)
        {
            try
            {
                var buf = converter.GetBytes(value);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = converter.Length;
                if (len > 0 && len != buf.Length)
                    throw PacketException.ConvertMismatch(len);
                return buf;
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static byte[] GetBytesWrap<T>(this IPacketConverter<T> converter, T value)
        {
            try
            {
                var buf = converter.GetBytes(value);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = converter.Length;
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
