using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    internal sealed class _Inf
    {
        internal const int Enum = 1;
        internal const int Array = 2;
        internal const int List = 4;
        internal const int Enumerable = 8;
        internal const int EnumerableImpl = 16;
        internal const int Collection = 32;
        internal const int Dictionary = 64;
        internal const int EnumerableKeyValuePair = 128;
        internal const int KeyValuePair = 256;

        internal int Flags { get; set; }

        internal Type ElementType { get; set; }

        internal Type IndexType { get; set; }

        internal Func<PacketReader, IPacketConverter, object> GetArray { get; set; }

        internal Func<PacketReader, IPacketConverter, object> GetList { get; set; }

        internal Func<PacketReader, IPacketConverter, object> GetCollection { get; set; }

        internal Func<PacketReader, IPacketConverter, object> GetEnumerable { get; set; }

        internal Func<PacketReader, int, object> GetEnumerableReader { get; set; }

        internal Func<PacketReader, IPacketConverter, IPacketConverter, object> GetDictionary { get; set; }

        internal Func<object[], object> CastToArray { get; set; }

        internal Func<object[], object> CastToList { get; set; }

        internal Func<object[], object> CastToCollection { get; set; }

        internal Func<IEnumerable<KeyValuePair<object, object>>, object> CastToDictionary { get; set; }

        internal Func<IPacketConverter, object, MemoryStream> FromEnumerable { get; set; }

        internal Func<IPacketConverter, IPacketConverter, object, MemoryStream> FromEnumerableKeyValuePair { get; set; }

        internal Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> GetEnumerableKeyValuePairAdapter { get; set; }
    }
}
