using System;
using System.Runtime.Serialization;
using System.Threading;

namespace Mikodev.Network
{
    [Serializable]
    public sealed class PacketException : Exception
    {
        private readonly PacketError _err = PacketError.None;

        private static string _GetMessage(PacketError code)
        {
            switch (code)
            {
                case PacketError.ConvertError:
                    return "Convert failed, see inner exception for more information";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.PathError:
                    return "Path not exists";
                case PacketError.RecursiveError:
                    return "Recursion limit has been reached";
                default:
                    return "Undefined error";
            }
        }

        public PacketError ErrorCode => _err;

        public PacketException(PacketError code) : base(_GetMessage(code)) => _err = code;

        public PacketException(PacketError code, string message) : base(message) => _err = code;

        public PacketException(PacketError code, Exception except) : base(_GetMessage(code), except) => _err = code;

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            _err = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), _err);
            base.GetObjectData(info, context);
        }

        internal static PacketException Overflow()
        {
            return new PacketException(PacketError.Overflow);
        }

        internal static PacketException ConvertError(Exception ex)
        {
            return new PacketException(PacketError.ConvertError, ex);
        }

        internal static PacketException ConvertMismatch(int length)
        {
            return new PacketException(PacketError.ConvertMismatch, $"Converter should return a byte array of length {length}");
        }

        internal static PacketException InvalidType(Type type)
        {
            return new PacketException(PacketError.InvalidType, $"Invalid type: {type}");
        }

        internal static PacketException InvalidKeyType(Type type)
        {
            return new PacketException(PacketError.InvalidKeyType, $"Invalid dictionary key type: {type}");
        }

        internal static bool WrapFilter(Exception ex)
        {
            if (ex is PacketException || ex is OutOfMemoryException || ex is StackOverflowException || ex is ThreadAbortException)
                return false;
            return true;
        }
    }
}
