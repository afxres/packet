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
        internal static object GetObjectChecked(this PacketConverter converter, Element element, bool check = false) => GetObjectChecked(converter, element.buffer, element.offset, element.length, check);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetValueChecked<T>(this PacketConverter<T> converter, Element element, bool check = false) => GetValueChecked(converter, element.buffer, element.offset, element.length, check);

        internal static object GetObjectChecked(this PacketConverter converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                return converter.GetObject(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        internal static T GetValueChecked<T>(this PacketConverter<T> converter, byte[] buffer, int offset, int length, bool check = false)
        {
            try
            {
                if (check && converter.Length > length)
                    throw PacketException.Overflow();
                return converter.GetValue(buffer, offset, length);
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }
        #endregion

        #region object -> bytes
        internal static byte[] GetBytesChecked(this PacketConverter converter, object value)
        {
            try
            {
                var buffer = converter.GetBytes(value);
                if (buffer == null)
                    buffer = UnmanagedArrayConverter<byte>.EmptyArray;
                var define = converter.Length;
                if (define > 0 && define != buffer.Length)
                    throw PacketException.ConversionMismatch(define);
                return buffer;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        internal static byte[] GetBytesChecked<T>(this PacketConverter<T> converter, T value)
        {
            try
            {
                var buffer = converter.GetBytes(value);
                if (buffer == null)
                    buffer = UnmanagedArrayConverter<byte>.EmptyArray;
                var define = converter.Length;
                if (define > 0 && define != buffer.Length)
                    throw PacketException.ConversionMismatch(define);
                return buffer;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }
        #endregion
    }
}
