﻿using System;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据处理时由于数据长度不匹配, 数据无效, 类型转换失败等情况引发的异常.
    /// </summary>
    [Serializable]
    public class PacketException : Exception
    {
        public PacketException() { }
        public PacketException(string message) : base(message) { }
        public PacketException(string message, Exception inner) : base(message, inner) { }
        protected PacketException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
