using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNull() => throw new ArgumentNullException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflow() => throw new OverflowException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConverterInitialized() => throw new InvalidOperationException("Converter already initialized!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConverterNotInitialized() => throw new InvalidOperationException("Converter not initialized!");
    }
}
