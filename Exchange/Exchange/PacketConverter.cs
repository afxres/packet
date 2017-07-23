using BinaryFunction = System.Func<object, byte[]>;
using ObjectFunction = System.Func<byte[], int, int, object>;

namespace Mikodev.Network
{
    /// <summary>
    /// Binary converter
    /// </summary>
    public class PacketConverter
    {
        private BinaryFunction _push = null;
        private ObjectFunction _pull = null;
        private int? _size = null;

        /// <summary>
        /// object -> byte[]
        /// </summary>
        public BinaryFunction BinaryFunction => _push;

        /// <summary>
        /// byte[] -> object
        /// </summary>
        public ObjectFunction ObjectFunction => _pull;

        /// <summary>
        /// Length of current type, null if not constant
        /// </summary>
        public int? Length => _size;

        /// <summary>
        /// Initialize new converter
        /// </summary>
        /// <param name="binaryFunction">object -> byte[]</param>
        /// <param name="objectFunction">byte[] -> object</param>
        /// <param name="length"></param>
        public PacketConverter(BinaryFunction binaryFunction, ObjectFunction objectFunction, int? length)
        {
            _push = binaryFunction;
            _pull = objectFunction;
            _size = length;
        }
    }
}
