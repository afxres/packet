using System;

namespace Mikodev.Network.Extensions
{
    public static partial class PacketExtensions
    {
        /// <summary>
        /// 判断类型是否为值类型
        /// </summary>
        public static bool IsValueType(this Type type) => type.IsValueType;
    }
}
