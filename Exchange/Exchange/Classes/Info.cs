using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class Info
    {
        internal const int None = 0;
        internal const int Enum = 128;

        internal const int Reader = 16;
        internal const int RawReader = 17;
        internal const int Collection = 18;
        internal const int Enumerable = 19;
        internal const int Dictionary = 20;
        internal const int Map = 21; // Dictionary<string, object>
        internal const int Bytes = 22;
        internal const int SBytes = 23;
        internal const int Writer = 24;
        internal const int RawWriter = 25;

        internal int Flag { get; set; }

        internal int From { get; set; }

        internal int To { get; set; }

        internal Type Type { get; set; }

        internal Type ElementType { get; set; }

        internal Type IndexType { get; set; }

        internal Func<PacketReader, IPacketConverter, object> ToCollection { get; set; }

        internal Func<object[], object> ToCollectionCast { get; set; }

        internal Func<PacketReader, IPacketConverter, object> ToEnumerable { get; set; }

        internal Func<PacketReader, int, Info, object> ToEnumerableAdapter { get; set; }

        internal Func<PacketReader, IPacketConverter, IPacketConverter, object> ToDictionary { get; set; }

        internal Func<List<KeyValuePair<object, object>>, object> ToDictionaryCast { get; set; }

        internal Func<IPacketConverter, object, byte[][]> FromEnumerable { get; set; }

        internal Func<IPacketConverter, IPacketConverter, object, List<KeyValuePair<byte[], byte[]>>> FromDictionary { get; set; }

        internal Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> FromDictionaryAdapter { get; set; }
    }
}
