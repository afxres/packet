using System;

namespace Mikodev.Network.Internal
{
    internal partial class Extension
    {
        #region overload

        internal static T GetValue<T>(this PacketConverter<T> converter, Block block) => converter.GetValue(block.Buffer, block.Offset, block.Length);

        #endregion

        #region bytes -> object

        internal static object GetObjectChecked(this PacketConverter converter, Block block, bool check = false) => GetObjectChecked(converter, block.Buffer, block.Offset, block.Length, check);

        internal static T GetValueChecked<T>(this PacketConverter<T> converter, Block block, bool check = false) => GetValueChecked(converter, block.Buffer, block.Offset, block.Length, check);

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
                    buffer = Empty.Array<byte>();
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
                    buffer = Empty.Array<byte>();
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
