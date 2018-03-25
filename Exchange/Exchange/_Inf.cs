using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    internal sealed class _Inf
    {
        internal const int Enum = 1;
        internal const int Mapping = 2; // Dictionary<string, object>

        internal const int Array = 16;
        internal const int List = 17;
        internal const int Enumerable = 18;
        internal const int Collection = 19;
        internal const int Dictionary = 20;

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

        internal Func<IEnumerable<KeyValuePair<object, object>>, object> ToDictionaryCast { get; set; }

        internal Func<IPacketConverter, object, MemoryStream> FromEnumerable { get; set; }

        internal Func<IPacketConverter, IPacketConverter, object, MemoryStream> FromDictionary { get; set; }

        internal Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> FromDictionaryAdapter { get; set; }
    }
}
