using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    /// <summary>
    /// 扩展方法
    /// Extend functions
    /// </summary>
    public static class PacketModules
    {
        /// <summary>
        /// Get value from current node
        /// </summary>
        [Obsolete]
        public static T Pull<T>(this PacketReader reader, string key)
        {
            return reader._Item(key, false).Pull<T>();
        }

        /// <summary>
        /// Get value from current node
        /// </summary>
        [Obsolete]
        public static object Pull(this PacketReader reader, string key, Type type)
        {
            return reader._Item(key, false).Pull(type);
        }

        /// <summary>
        /// Get value collection from current node
        /// </summary>
        [Obsolete]
        public static IEnumerable<T> PullList<T>(this PacketReader reader, string key, bool withLengthInfo = false)
        {
            return reader._Item(key, false)._ListGeneric<T>(withLengthInfo);
        }

        /// <summary>
        /// Get value collection from current node
        /// </summary>
        [Obsolete]
        public static IEnumerable PullList(this PacketReader reader, string key, Type type, bool withLengthInfo = false)
        {
            return reader._Item(key, false)._List(type, withLengthInfo);
        }
    }
}
