﻿using System;
using System.Runtime.Serialization;

namespace Mikodev.Binary
{
    [Serializable]
    public class PacketException : Exception
    {
        public PacketException() { }

        public PacketException(string message) : base(message) { }

        public PacketException(string message, Exception inner) : base(message, inner) { }

        protected PacketException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
