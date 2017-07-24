using BinaryFunction = System.Func<object, byte[]>;
using ObjectFunction = System.Func<byte[], int, int, object>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary converter
    /// </summary>
    public class PacketConverter
    {
        private BinaryFunction _bin = null;
        private ObjectFunction _obj = null;
        private int? _len = null;

        /// <summary>
        /// object -> byte[]
        /// </summary>
        public BinaryFunction ToBinary => _bin;

        /// <summary>
        /// byte[] -> object
        /// </summary>
        public ObjectFunction ToObject => _obj;

        /// <summary>
        /// Length of current type, null if not constant
        /// </summary>
        public int? Length => _len;

        /// <summary>
        /// Initialize new converter
        /// </summary>
        /// <param name="bin">object -> byte[]</param>
        /// <param name="obj">byte[] -> object</param>
        /// <param name="length"></param>
        public PacketConverter(BinaryFunction bin, ObjectFunction obj, int? length)
        {
            _bin = bin;
            _obj = obj;
            _len = length;
        }
    }
}
