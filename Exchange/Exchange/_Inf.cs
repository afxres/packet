using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _Inf
    {
        internal const int None = 0;
        internal const int Enum = 126;

        internal const int Collection = 16;
        internal const int Array = 17;
        internal const int List = 18;
        internal const int Enumerable = 19;
        internal const int Dictionary = 20;
        internal const int Mapping = 21; // Dictionary<string, object>

        internal int Flag { get; set; }

        internal int From { get; set; }

        internal int To { get; set; }

        internal Type ElementType { get; set; }

        internal Type IndexerType { get; set; }

        internal Func<PacketReader, IPacketConverter, object> ToCollection { get; set; }

        internal Func<object[], object> ToCollectionCast { get; set; }

        internal Func<PacketReader, IPacketConverter, object> ToEnumerable { get; set; }

        internal Func<PacketReader, int, object> ToEnumerableAdapter { get; set; }

        internal Func<PacketReader, IPacketConverter, IPacketConverter, object> ToDictionary { get; set; }

        internal Func<List<KeyValuePair<object, object>>, object> ToDictionaryCast { get; set; }

        internal Func<IPacketConverter, object, byte[][]> FromEnumerable { get; set; }

        internal Func<IPacketConverter, IPacketConverter, object, List<KeyValuePair<byte[], byte[]>>> FromDictionary { get; set; }

        internal Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> FromDictionaryAdapter { get; set; }
    }
}
