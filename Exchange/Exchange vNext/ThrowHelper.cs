using System;

namespace Mikodev.Binary
{
    internal static class ThrowHelper
    {
        internal static void ThrowEmptyBlock() => throw new InvalidOperationException("Block is empty");

        internal static void ThrowEmptyAllocator() => throw new InvalidOperationException("Allocator is empty");
    }
}
