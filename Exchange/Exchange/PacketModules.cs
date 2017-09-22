using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    /// <summary>
    /// Obsolete functions
    /// </summary>
    public static class PacketModules
    {
        /// <summary>
        /// Get value from current node
        /// </summary>
        [Obsolete]
        public static object Pull(this PacketReader reader, string key, Type type)
        {
            return reader._Item(key, false).Pull(type);
        }

        /// <summary>
        /// Get value from current node
        /// </summary>
        [Obsolete]
        public static T Pull<T>(this PacketReader reader, string key)
        {
            return reader._Item(key, false).Pull<T>();
        }

        /// <summary>
        /// Get value collection from current node
        /// </summary>
        [Obsolete]
        public static IEnumerable PullList(this PacketReader reader, string key, Type type)
        {
            return reader._Item(key, false).PullList(type);
        }

        /// <summary>
        /// Get value collection from current node
        /// </summary>
        [Obsolete]
        public static IEnumerable<T> PullList<T>(this PacketReader reader, string key)
        {
            return reader._Item(key, false).PullList<T>();
        }
    }
}
