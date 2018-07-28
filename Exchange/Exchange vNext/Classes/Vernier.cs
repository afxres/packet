using Mikodev.Binary.Converters;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal struct Vernier
    {
        internal readonly int limits;
        internal int offset;
        internal int length;

        internal Vernier(int limits)
        {
            this.limits = limits;
            offset = 0;
            length = 0;
        }

        internal bool Any() => limits - offset != length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Flush(in byte location)
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                ThrowHelper.ThrowOverflow();
            int length;
            fixed (byte* pointer = &location)
                length = UnmanagedValueConverter<int>.UnsafeToValue(pointer + offset);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                ThrowHelper.ThrowOverflow();
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FlushExcept(in byte location, int define)
        {
            if (define == 0)
            {
                Flush(in location);
            }
            else
            {
                offset += length;
                if ((uint)(limits - offset) < (uint)define)
                    ThrowHelper.ThrowOverflow();
                length = define;
            }
        }
    }
}
