using System;
using System.Runtime.Serialization;

namespace Mikodev.Network
{
    /// <summary>
    /// Exception cause by overflow, converter not found, etc
    /// </summary>
    [Serializable]
    public class PacketException : Exception
    {
        internal readonly PacketError _code = PacketError.None;

        internal static string _Message(PacketError code)
        {
            switch (code)
            {
                case PacketError.PathError:
                    return "Path not exists";
                case PacketError.TypeInvalid:
                    return "Invalid type";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.RecursiveError:
                    return "Recursion limit has been reached";
                case PacketError.AssertFailed:
                    return "Assert failed";
                case PacketError.ConvertError:
                    return "Convert failed, see inner exception for more information";
                default:
                    return "Undefined error";
            }
        }

        /// <summary>
        /// Error code
        /// </summary>
        public PacketError ErrorCode => _code;

        /// <summary>
        /// Create new instance with error code
        /// </summary>
        public PacketException(PacketError code) : base(_Message(code)) => _code = code;

        /// <summary>
        /// Create new instance with error code and inner exception
        /// </summary>
        public PacketException(PacketError code, Exception except) : base(_Message(code), except) => _code = code;

        /// <summary>
        /// Default exception method
        /// </summary>
        protected PacketException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
