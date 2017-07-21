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
        public PacketException(PacketError code) : base(_GetMessage(code)) => _code = code;

        /// <summary>
        /// 创建异常对象 并设置异常信息和内部异常
        /// </summary>
        public PacketException(PacketError code, Exception inner) : base(_GetMessage(code), inner) => _code = code;

        internal static string _GetMessage(PacketError code)
        {
            switch (code)
            {
                case PacketError.KeyNotFound:
                    return "键不存在";
                case PacketError.InvalidType:
                    return "类型无效";
                case PacketError.Overflow:
                    return "长度超出范围";
                case PacketError.RecursiveError:
                    return "递归深度超过限制";
                default:
                    return "未提供错误信息";
            }
        }
    }
}
