namespace Mikodev.Network
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public enum PacketError
    {
        /// <summary>
        /// 默认值
        /// </summary>
        None,

        /// <summary>
        /// 路径错误
        /// </summary>
        PathError,

        /// <summary>
        /// 类型无效
        /// </summary>
        TypeInvalid,

        /// <summary>
        /// 长度溢出
        /// </summary>
        Overflow,

        /// <summary>
        /// 递归深度超过限制
        /// </summary>
        RecursiveError,
    }
}
