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
        /// 键不存在
        /// </summary>
        KeyNotFound,

        /// <summary>
        /// 类型无效
        /// </summary>
        InvalidType,

        /// <summary>
        /// 数据长度溢出
        /// </summary>
        Overflow,

        /// <summary>
        /// 递归深度超过限制
        /// </summary>
        RecursiveError,
    }
}
