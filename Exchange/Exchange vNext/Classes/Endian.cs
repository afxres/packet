using System.Runtime.CompilerServices;
using intptr = System.IntPtr;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Binary
{
    internal static class Endian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint16 Reverse16(uint16 value)
        {
            return (uint16)((value >> 8) + (value << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint32 Reverse32(uint32 value)
        {
            var one = value & 0x00FF00FFU;
            var two = value & 0xFF00FF00U;
            return ((one >> 8) | one << 24) + (two << 8 | (two >> 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64 Reverse64(uint64 value)
        {
            return ((uint64)Reverse32((uint32)value) << 32) + Reverse32((uint32)(value >> 32));
        }

        internal static T Reverse<T>(T value) where T : unmanaged
        {
            var size = Unsafe.SizeOf<T>();
            if (size == sizeof(uint16))
                Unsafe.As<T, uint16>(ref value) = Reverse16(Unsafe.As<T, uint16>(ref value));
            else if (size == sizeof(uint32))
                Unsafe.As<T, uint32>(ref value) = Reverse32(Unsafe.As<T, uint32>(ref value));
            else if (size == sizeof(uint64))
                Unsafe.As<T, uint64>(ref value) = Reverse64(Unsafe.As<T, uint64>(ref value));
            else
                throw new System.ApplicationException();
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReverseRange<T>(ref T location, int length) where T : unmanaged => ReverseRange<T>(ref Unsafe.As<T, byte>(ref location), length);

        internal static void ReverseRange<T>(ref byte location, int length) where T : unmanaged
        {
            var size = Unsafe.SizeOf<T>();
            if (size == sizeof(uint16))
                for (var i = 0; i < length; i += size)
                    Unsafe.As<byte, uint16>(ref Unsafe.AddByteOffset(ref location, (intptr)i)) = Reverse16(Unsafe.As<byte, uint16>(ref Unsafe.AddByteOffset(ref location, (intptr)i)));
            else if (size == sizeof(uint32))
                for (var i = 0; i < length; i += size)
                    Unsafe.As<byte, uint32>(ref Unsafe.AddByteOffset(ref location, (intptr)i)) = Reverse32(Unsafe.As<byte, uint32>(ref Unsafe.AddByteOffset(ref location, (intptr)i)));
            else if (size == sizeof(uint64))
                for (var i = 0; i < length; i += size)
                    Unsafe.As<byte, uint64>(ref Unsafe.AddByteOffset(ref location, (intptr)i)) = Reverse64(Unsafe.As<byte, uint64>(ref Unsafe.AddByteOffset(ref location, (intptr)i)));
            else
                throw new System.ApplicationException();
        }
    }
}
