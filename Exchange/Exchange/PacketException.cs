using System;
using System.Runtime.Serialization;

namespace Mikodev.Network
{
    [Serializable]
    public sealed class PacketException : Exception
    {
        internal readonly PacketError _code = PacketError.None;

        internal static string _GetMessage(PacketError code)
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
                case PacketError.InvalidType:
                    return "Invalid type";
                default:
                    return "Undefined error";
            }
        }

        public PacketError ErrorCode => _code;

        public PacketException(PacketError code) : base(_GetMessage(code)) => _code = code;

        public PacketException(PacketError code, Exception except) : base(_GetMessage(code), except) => _code = code;

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            _code = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), _code);
            base.GetObjectData(info, context);
        }
    }
}
