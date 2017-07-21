using System;

namespace Mikodev.Network
{
    /// <summary>
    /// 扩展方法
    /// Extend functions
    /// </summary>
    public static class PacketModules
    {
        /// <summary>
        /// Pull byte array from current node 
        /// <para>Please use <see cref="PacketReader.Pull{T}"/> (T is <see cref="byte"/>[])</para>
        /// </summary>
        [Obsolete]
        public static byte[] PullList(this PacketReader reader)
        {
            return reader._buf.Split(reader._off, reader._len);
        }

        /// <summary>
        /// Pull value from current node
        /// <para>Please use <see cref="PacketReader"/>[<see cref="String"/> key] and <see cref="PacketReader.Pull{T}"/></para>
        /// </summary>
        [Obsolete]
        public static T Pull<T>(this PacketReader reader, string key)
        {
            return reader._Item(key, false).Pull<T>();
        }
    }
}
