using System;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据处理过程中, 因数据长度不匹配, 数据无效等情况引发的异常.
    /// </summary>
    public class PacketException : Exception
    {
        internal PacketError _code = PacketError.None;

        /// <summary>
        /// 错误代码
        /// </summary>
        public PacketError ErrorCode => _code;

        /// <summary>
        /// 创建异常对象 并设置异常信息
        /// </summary>
        public PacketException(PacketError code) : base(_Message(code)) => _code = code;

        /// <summary>
        /// 创建异常对象 并设置异常信息和内部异常
        /// </summary>
        public PacketException(PacketError code, Exception inner) : base(_Message(code), inner) => _code = code;

        internal static string _Message(PacketError code)
        {
            switch (code)
            {
                case PacketError.PathError:
                    return "Path not exists";
                case PacketError.InvalidType:
                    return "Invalid type";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.RecursiveError:
                    return "Recursion limit has been reached";
                default:
                    return "Unknown error";
            }
        }
    }
}
