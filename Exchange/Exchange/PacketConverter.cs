using System;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary converter
    /// </summary>
    public class PacketConverter
    {
        internal readonly Func<object, byte[]> _bin;
        internal readonly Func<byte[], int, int, object> _obj;
        internal readonly int? _len;

        /// <summary>
        /// object -> byte[]
        /// </summary>
        /// <exception cref="PacketException"></exception>
        public byte[] ToBinary(object value)
        {
            try
            {
                return _bin.Invoke(value);
            }
            catch (Exception ex)
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }

        /// <summary>
        /// byte[] -> object
        /// </summary>
        /// <exception cref="PacketException"></exception>
        public object ToObject(byte[] buffer, int offset, int length)
        {
            try
            {
                return _obj.Invoke(buffer, offset, length);
            }
            catch (Exception ex)
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }

        /// <summary>
        /// Length of current type, null if not constant
        /// </summary>
        public int? Length => _len;

        /// <summary>
        /// Initialize new converter
        /// </summary>
        /// <param name="bin">object -> byte[]</param>
        /// <param name="obj">byte[] -> object</param>
        /// <param name="length">Byte length, null if not constant</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public PacketConverter(Func<object, byte[]> bin, Func<byte[], int, int, object> obj, int? length)
        {
            if (length is int len && len < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            _bin = bin ?? throw new ArgumentNullException(nameof(bin));
            _obj = obj ?? throw new ArgumentNullException(nameof(obj));
            _len = length;
        }
    }
}
