using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNull() => throw new ArgumentNullException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflow() => throw new OverflowException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConverterLengthOutOfRange() => throw new ArgumentOutOfRangeException("Length must be greater or equal to zero!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConverterInitialized() => throw new InvalidOperationException("Converter already initialized!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConverterNotInitialized() => throw new InvalidOperationException("Converter not initialized!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAllocatorModified() => throw new InvalidOperationException("Allocator has been modified!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowKeyNotFoundException<T>() => throw new KeyNotFoundException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowOverflowOrNull<T>()
        {
            if (default(T) == null)
                return default;
            throw new OverflowException();
        }
    }
}
