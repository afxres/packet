using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Assign<T>(ref byte location, T value) where T : unmanaged
        {
            fixed (byte* pointer = &location)
                *(T*)pointer = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T As<T>(in byte location) where T : unmanaged
        {
            fixed (byte* pointer = &location)
                return *(T*)pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Copy<T, U>(ref T target, in U source, int count) where T : unmanaged where U : unmanaged
        {
            fixed (T* dst = &target)
            fixed (U* src = &source)
                Buffer.MemoryCopy(src, dst, count, count);
        }
    }
}
