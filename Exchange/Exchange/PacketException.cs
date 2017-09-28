﻿using System;
using System.Runtime.Serialization;

namespace Mikodev.Network
{
    /// <summary>
    /// Exception cause by overflow, converter not found, etc
    /// </summary>
    [Serializable]
    public sealed class PacketException : Exception
    {
        internal readonly PacketError _code = PacketError.None;

        internal static string _Message(PacketError code)
        {
            switch (code)
            {
                case PacketError.AssertFailed:
                    return "Assert failed";
                case PacketError.ConvertError:
                    return "Convert failed, see inner exception for more information";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.PathError:
                    return "Path not exists";
                case PacketError.RecursiveError:
                    return "Recursion limit has been reached";
                case PacketError.TypeInvalid:
                    return "Invalid type";
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

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            _code = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        /// <summary>
        /// Provide object for serialization
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), _code);
            base.GetObjectData(info, context);
        }
    }
}
