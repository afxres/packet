namespace Mikodev.Network
{
    /// <summary>
    /// 错误代码. Error code
    /// </summary>
    public enum PacketError
    {
        /// <summary>
        /// 默认值. Default
        /// </summary>
        None,

        /// <summary>
        /// 转换器异常. Convert operation error
        /// </summary>
        ConvertError,

        /// <summary>
        /// 数据长度溢出. Data length overflow
        /// </summary>
        Overflow,

        /// <summary>
        /// 路径错误. Path error
        /// </summary>
        PathError,

        /// <summary>
        /// 递归深度已达上限. Recursion limit has been reached
        /// </summary>
        RecursiveError,

        /// <summary>
        /// 无效的类型. Type invalid
        /// </summary>
        TypeInvalid,
    }
}
