using Mikodev.Network.Converters;
using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    partial class Extension
    {
        #region bytes -> object overload
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetValue<T>(this PacketConverter<T> converter, Element element) => converter.GetValue(element.buffer, element.offset, element.length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object GetObject(this PacketConverter converter, Element element) => converter.GetObject(element.buffer, element.offset, element.length);
        #endregion

        #region bytes -> object
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object GetObjectWrap(this PacketConverter converter, Element element, bool check = false) => GetObjectWrap(converter, element.buffer, element.offset, element.length, check);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetValueWrapAuto<T>(this PacketConverter converter, Element element, bool check = false) => GetValueWrapAuto<T>(converter, element.buffer, element.offset, element.length, check);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetValueWrap<T>(this PacketConverter<T> converter, Element element) => GetValueWrap(converter, element.buffer, element.offset, element.length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        #endregion

        #region object -> bytes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] GetBytesWrap(this PacketConverter converter, object value)
        {
            try
            {
                var buf = converter.GetBytes(value);
                if (buf == null)
                    buf = UnmanagedArrayConverter<byte>.EmptyArray;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] GetBytesWrap<T>(this PacketConverter<T> converter, T value)
        {
            try
            {
                var buf = converter.GetBytes(value);
                if (buf == null)
                    buf = UnmanagedArrayConverter<byte>.EmptyArray;
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
