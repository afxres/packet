using System;
using BinaryFunction = System.Func<object, byte[]>;
using ObjectFunction = System.Func<byte[], int, int, object>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary converter
    /// </summary>
    public class PacketConverter
    {
        internal readonly BinaryFunction _bin;
        internal readonly ObjectFunction _obj;
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
        public PacketConverter(BinaryFunction bin, ObjectFunction obj, int? length)
        {
            if (bin == null || obj == null)
                throw new ArgumentNullException();
            if (length is int len && len < 0)
                throw new ArgumentOutOfRangeException();
            _bin = bin;
            _obj = obj;
            _len = length;
        }
    }
}
