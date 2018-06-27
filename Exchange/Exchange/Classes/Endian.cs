using System;
using System.Runtime.CompilerServices;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Network
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
            uint32 one = value & 0x00FF00FFU;
            uint32 two = value & 0xFF00FF00U;
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
                    var uint16 = Reverse16(Unsafe.As<T, uint16>(ref value));
                    return Unsafe.As<uint16, T>(ref uint16);
                case sizeof(uint32):
                    var uint32 = Reverse32(Unsafe.As<T, uint32>(ref value));
                    return Unsafe.As<uint32, T>(ref uint32);
                case sizeof(uint64):
                    var uint64 = Reverse64(Unsafe.As<T, uint64>(ref value));
                    return Unsafe.As<uint64, T>(ref uint64);
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

        internal static void ReverseBlock<T>(byte[] buffer) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(uint16):
                    for (int i = 0; i < buffer.Length; i += sizeof(uint16))
                        Unsafe.As<byte, uint16>(ref buffer[i]) = Reverse16(Unsafe.As<byte, uint16>(ref buffer[i]));
                    break;
                case sizeof(uint32):
                    for (int i = 0; i < buffer.Length; i += sizeof(uint32))
                        Unsafe.As<byte, uint32>(ref buffer[i]) = Reverse32(Unsafe.As<byte, uint32>(ref buffer[i]));
                    break;
                case sizeof(uint64):
                    for (int i = 0; i < buffer.Length; i += sizeof(uint64))
                        Unsafe.As<byte, uint64>(ref buffer[i]) = Reverse64(Unsafe.As<byte, uint64>(ref buffer[i]));
                    break;
                default:
                    throw new ApplicationException();
            }
        }
    }
}
