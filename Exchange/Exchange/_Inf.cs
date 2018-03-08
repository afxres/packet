using System;

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

        internal Type ElementType { get; set; }

        internal Type EnumerableElementType { get; set; }

        internal Func<PacketReader, object> CollectionFunction { get; set; }

        internal int Flags { get; set; }
    }
}
