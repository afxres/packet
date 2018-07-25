using Mikodev.Binary.Converters;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal struct Vernier
    {
        internal static MethodInfo FlushExceptMethodInfo { get; } = typeof(Vernier).GetMethod(nameof(FlushExcept), BindingFlags.Instance | BindingFlags.NonPublic);

        internal readonly Memory<byte> memory;
        internal int offset;
        internal int length;

        internal Vernier(Memory<byte> memory)
        {
            this.memory = memory;
            offset = 0;
            length = 0;
        }

        internal bool Any() => memory.Length - offset != length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Flush()
        {
            offset += this.length;
            if ((uint)(memory.Length - offset) < sizeof(int))
                ThrowHelper.ThrowOverflow();
            var length = UnmanagedValueConverter<int>.UnsafeToValue(ref memory.Span[offset]);
            offset += sizeof(int);
            if ((uint)(memory.Length - offset) < (uint)length)
                ThrowHelper.ThrowOverflow();
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FlushExcept(int define)
        {
            if (define == 0)
            {
                Flush();
            }
            else
            {
                offset += length;
                if ((uint)(memory.Length - offset) < (uint)define)
                    ThrowHelper.ThrowOverflow();
                length = define;
            }
        }

        public static explicit operator Vernier(Memory<byte> memory) => new Vernier(memory);

        public static explicit operator Memory<byte>(Vernier vernier) => vernier.memory.Slice(vernier.offset, vernier.length);
    }
}
