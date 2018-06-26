using System;
using System.Runtime.CompilerServices;
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
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    var u16 = Reverse16(Unsafe.As<T, uint16>(ref value));
                    return Unsafe.As<uint16, T>(ref u16);
                case sizeof(uint32):
                    var u32 = Reverse32(Unsafe.As<T, uint32>(ref value));
                    return Unsafe.As<uint32, T>(ref u32);
                case sizeof(uint64):
                    var u64 = Reverse64(Unsafe.As<T, uint64>(ref value));
                    return Unsafe.As<uint64, T>(ref u64);
                default:
                    throw new ApplicationException();
            }
        }

        internal static void ReverseArray<T>(T[] array) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, uint16>(ref array[i]) = Reverse16(Unsafe.As<T, uint16>(ref array[i]));
                    break;
                case sizeof(uint32):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, uint32>(ref array[i]) = Reverse32(Unsafe.As<T, uint32>(ref array[i]));
                    break;
                case sizeof(uint64):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, uint64>(ref array[i]) = Reverse64(Unsafe.As<T, uint64>(ref array[i]));
                    break;
                default:
                    throw new ApplicationException();
            }
        }

        internal static void ReverseBlock<T>(Block block) where T : unmanaged
        {
            var buffer = block.Buffer;
            var limits = block.Offset + block.Length;
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    for (int i = block.Offset; i < limits; i += sizeof(uint16))
                        Unsafe.As<byte, uint16>(ref buffer[i]) = Reverse16(Unsafe.As<byte, uint16>(ref buffer[i]));
                    break;
                case sizeof(uint32):
                    for (int i = block.Offset; i < limits; i += sizeof(uint32))
                        Unsafe.As<byte, uint32>(ref buffer[i]) = Reverse32(Unsafe.As<byte, uint32>(ref buffer[i]));
                    break;
                case sizeof(uint64):
                    for (int i = block.Offset; i < limits; i += sizeof(uint64))
                        Unsafe.As<byte, uint64>(ref buffer[i]) = Reverse64(Unsafe.As<byte, uint64>(ref buffer[i]));
                    break;
                default:
                    throw new ApplicationException();
            }
        }
    }
}
