namespace Mikodev.Network.Internal
{
    internal static class Unsafe
    {
        internal static unsafe void Copy<T, U>(ref T target, in U source, int length) where T : unmanaged where U : unmanaged
        {
            fixed (T* dst = &target)
            fixed (U* src = &source)
                Copy((byte*)dst, (byte*)src, length);
        }

        internal static unsafe void Copy(byte* target, byte* source, int length)
        {
            while (length >= 8)
            {
                *(ulong*)target = *(ulong*)source;
                target += 8;
                source += 8;
                length -= 8;
            }
            if (length >= 4)
            {
                *(uint*)target = *(uint*)source;
                target += 4;
                source += 4;
                length -= 4;
            }
            if (length >= 2)
            {
                *(ushort*)target = *(ushort*)source;
                target += 2;
                source += 2;
                length -= 2;
            }
            if (length >= 1)
            {
                *target = *source;
            }
        }
    }
}
