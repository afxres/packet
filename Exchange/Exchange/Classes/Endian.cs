using System.Runtime.CompilerServices;
using intptr = System.IntPtr;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Network
{
    internal static class Endian
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint16 Swap16(uint16 value)
        {
            return (uint16)((value >> 8) + (value << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint32 Swap32(uint32 value)
        {
            var one = value & 0x00FF00FFU;
            var two = value & 0xFF00FF00U;
            return ((one >> 8) | one << 24) + (two << 8 | (two >> 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64 Swap64(uint64 value)
        {
            return ((uint64)Swap32((uint32)value) << 32) + Swap32((uint32)(value >> 32));
        }

        internal static void Swap<T>(ref byte target, ref byte source) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    Unsafe.WriteUnaligned(ref target, Swap16(Unsafe.As<byte, uint16>(ref source)));
                    break;
                case sizeof(uint32):
                    Unsafe.WriteUnaligned(ref target, Swap32(Unsafe.As<byte, uint32>(ref source)));
                    break;
                case sizeof(uint64):
                    Unsafe.WriteUnaligned(ref target, Swap64(Unsafe.As<byte, uint64>(ref source)));
                    break;
                default:
                    throw new System.ApplicationException();
            }
        }

        internal static void SwapRange<T>(ref byte target, ref byte source, int byteCount) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    for (var i = 0; i < byteCount; i += sizeof(uint16))
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (intptr)i), Swap16(Unsafe.As<byte, uint16>(ref Unsafe.AddByteOffset(ref source, (intptr)i))));
                    break;
                case sizeof(uint32):
                    for (var i = 0; i < byteCount; i += sizeof(uint32))
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (intptr)i), Swap32(Unsafe.As<byte, uint32>(ref Unsafe.AddByteOffset(ref source, (intptr)i))));
                    break;
                case sizeof(uint64):
                    for (var i = 0; i < byteCount; i += sizeof(uint64))
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (intptr)i), Swap64(Unsafe.As<byte, uint64>(ref Unsafe.AddByteOffset(ref source, (intptr)i))));
                    break;
                default:
                    throw new System.ApplicationException();
            }
        }
    }
}
