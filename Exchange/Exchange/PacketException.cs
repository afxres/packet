using System;

namespace Mikodev.Network
{
    /// <summary>
    /// 数据处理时由于数据长度不匹配, 数据无效等情况引发的异常.
    /// </summary>
    public class PacketException : Exception
    {
        internal PacketErrorCode _code = PacketErrorCode.None;

        /// <summary>
        /// 错误代码
        /// </summary>
        public PacketErrorCode ErrorCode => _code;

        /// <summary>
        /// 创建异常对象 并设置异常信息
        /// </summary>
        public PacketException(PacketErrorCode code) : base(_GetMessage(code)) => _code = code;

        /// <summary>
        /// 创建异常对象 并设置异常信息和内部异常
        /// </summary>
        public PacketException(PacketErrorCode code, Exception inner) : base(_GetMessage(code), inner) => _code = code;

        internal static string _GetMessage(PacketErrorCode code)
        {
            switch (code)
            {
                case PacketErrorCode.KeyNotFound:
                    return "键不存在";
                case PacketErrorCode.InvalidType:
                    return "类型无效";
                case PacketErrorCode.LengthOverflow:
                    return "长度超出范围";
                default:
                    return "未提供错误信息";
            }
        }
    }
}
