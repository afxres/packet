using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Mikodev.Network
{
    [Serializable]
    public sealed class PacketException : Exception
    {
        private readonly PacketError error = PacketError.None;

        private static string GetMessage(PacketError code)
        {
            switch (code)
            {
                case PacketError.ConversionError:
                    return "See inner exception for more information";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.InvalidPath:
                    return "Path not exists";
                default:
                    return "Undefined error";
            }
        }

        public PacketError ErrorCode => error;

        internal PacketException(PacketError code) : base(GetMessage(code)) => error = code;

        internal PacketException(PacketError code, string message) : base(message) => error = code;

        internal PacketException(PacketError code, Exception exception) : base(GetMessage(code), exception) => error = code;

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            error = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), error);
            base.GetObjectData(info, context);
        }

        internal static PacketException ConversionError(Exception exception)
        {
            return new PacketException(PacketError.ConversionError, exception);
        }

        internal static PacketException ConversionMismatch(int length)
        {
            return new PacketException(PacketError.ConversionMismatch, $"Converter should return a byte array of length {length}");
        }

        internal static PacketException InvalidKeyType(Type type)
        {
            return new PacketException(PacketError.InvalidKeyType, $"Invalid dictionary key type: {type}");
        }

        internal static PacketException InvalidType(Type type)
        {
            return new PacketException(PacketError.InvalidType, $"Invalid type: {type}");
        }

        internal static PacketException Overflow()
        {
            return new PacketException(PacketError.Overflow);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static void VerifyRecursionError(ref int level)
        {
            const int limits = 64;
            if (level > limits)
                throw new PacketException(PacketError.RecursionError, $"Recursion limit of {limits} reached");
            level++;
        }

        internal static bool WrapFilter(Exception exception)
        {
            if (exception is PacketException || exception is OutOfMemoryException || exception is StackOverflowException || exception is ThreadAbortException)
                return false;
            return true;
        }
    }
}
