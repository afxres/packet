using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    partial class Extension
    {
        #region bytes -> object overload
#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValue<T>(this PacketConverter<T> converter, Element element)
        {
            return converter.GetValue(element.buffer, element.offset, element.length);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetObject(this PacketConverter converter, Element element)
        {
            return converter.GetObject(element.buffer, element.offset, element.length);
        }
        #endregion

        #region bytes -> object
#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetObjectWrap(this PacketConverter converter, Element element, bool check = false)
        {
            return GetObjectWrap(converter, element.buffer, element.offset, element.length, check);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static object GetObjectWrap(this PacketConverter converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                return converter.GetObject(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrapAuto<T>(this PacketConverter converter, Element element, bool check = false)
        {
            return GetValueWrapAuto<T>(converter, element.buffer, element.offset, element.length, check);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrapAuto<T>(this PacketConverter converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                if (converter is PacketConverter<T> res)
                    return res.GetValue(buffer, offset, length);
                return (T)converter.GetObject(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrap<T>(this PacketConverter<T> converter, byte[] buffer, int offset, int length)
        {
            try
            {
                return converter.GetValue(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static T GetValueWrap<T>(this PacketConverter<T> converter, Element element)
        {
            try
            {
                return converter.GetValue(element.buffer, element.offset, element.length);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }
        #endregion

        #region object -> bytes
#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static byte[] GetBufferWrap(this PacketConverter converter, object value)
        {
            try
            {
                var buf = converter.GetBuffer(value);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = converter.Length;
                if (len > 0 && len != buf.Length)
                    throw PacketException.ConversionMismatch(len);
                return buf;
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static byte[] GetBytesWrap<T>(this PacketConverter<T> converter, T value)
        {
            try
            {
                var buf = converter.GetBytes(value);
                if (buf == null)
                    buf = s_empty_bytes;
                var len = converter.Length;
                if (len > 0 && len != buf.Length)
                    throw PacketException.ConversionMismatch(len);
                return buf;
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }
        #endregion
    }
}
