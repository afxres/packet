using System;
using System.Runtime.Serialization;
using System.Threading;

namespace Mikodev.Network
{
    [Serializable]
    public sealed class PacketException : Exception
    {
        private const int RecursionLimits = 64;

        private const string RecursionError = "Recursion limit of 64 reached";

        private static string GetMessage(PacketError code)
        {
            switch (code)
            {
                case PacketError.ConversionError:
                    return "See the inner exception for details";

                case PacketError.Overflow:
                    return "Data length overflow";

                case PacketError.InvalidPath:
                    return "Path does not exist";

                default:
                    return "Undefined error";
            }
        }

        public PacketError ErrorCode { get; private set; } = PacketError.None;

        internal PacketException(PacketError code) : base(GetMessage(code)) => this.ErrorCode = code;

        internal PacketException(PacketError code, string message) : base(message) => this.ErrorCode = code;

        internal PacketException(PacketError code, Exception exception) : base(GetMessage(code), exception) => this.ErrorCode = code;

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            this.ErrorCode = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), this.ErrorCode);
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

        internal static PacketException InvalidElementType(Type type, Type collectionType)
        {
            return new PacketException(PacketError.InvalidElementType, $"Invalid collection element type: {type} (collection type: {collectionType})");
        }

        internal static PacketException InvalidKeyType(Type type, Type dictionaryType)
        {
            return new PacketException(PacketError.InvalidKeyType, $"Invalid dictionary key type: {type} (dictionary type: {dictionaryType})");
        }

        internal static PacketException InvalidType(Type type)
        {
            return new PacketException(PacketError.InvalidType, $"Invalid type: {type}");
        }

        internal static PacketException Overflow()
        {
            return new PacketException(PacketError.Overflow);
        }

        internal static void VerifyRecursionError(ref int level)
        {
            if (level > RecursionLimits)
                throw new PacketException(PacketError.RecursionError, RecursionError);
            level++;
        }

        internal static bool ReThrowFilter(Exception exception)
        {
            return !(exception is PacketException || exception is OutOfMemoryException || exception is StackOverflowException || exception is ThreadAbortException);
        }
    }
}
