using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Copy(byte[] target, byte[] source, int length)
        {
            Buffer.BlockCopy(source, 0, target, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void Copy(void* target, void* source, int length)
        {
            Buffer.MemoryCopy(source, target, length, length);
        }
    }
}
