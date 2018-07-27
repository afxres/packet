using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Copy<T, U>(ref T target, in U source, int count) where T : unmanaged where U : unmanaged
        {
            fixed (T* dst = &target)
            fixed (U* src = &source)
                Buffer.MemoryCopy(src, dst, count, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Copy(byte* target, byte* source, int count)
        {
            Buffer.MemoryCopy(source, target, count, count);
        }
    }
}
