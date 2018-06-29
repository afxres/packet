using System;

namespace Mikodev.Binary
{
    internal static class ThrowHelper
    {
        internal static void ThrowEmptyBlock() => throw new InvalidOperationException("Block is empty");

        internal static void ThrowEmptyAllocator() => throw new InvalidOperationException("Allocator is empty");

        internal static void ThrowArgumentNull() => throw new ArgumentNullException();

        internal static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException();
    }
}
