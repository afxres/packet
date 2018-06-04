using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network
{
    partial class Extension
    {
        internal const string UnableToReverseEndianness = "Unable to reverse endianness";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt16 ReverseEndianness(UInt16 value)
        {
            return (UInt16)((value >> 8) + (value << 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt32 ReverseEndianness(UInt32 value)
        {
            UInt32 one = value & 0x00FF00FFU;
            UInt32 two = value & 0xFF00FF00U;
            return ((one >> 8) | one << 24) + (two << 8 | (two >> 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UInt64 ReverseEndianness(UInt64 value)
        {
            return ((UInt64)ReverseEndianness((UInt32)value) << 32) + ReverseEndianness((UInt32)(value >> 32));
        }

        internal static T ReverseEndianness<T>(T value) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(UInt16):
                    var bit16 = ReverseEndianness(Unsafe.As<T, UInt16>(ref value));
                    return Unsafe.As<UInt16, T>(ref bit16);
                case sizeof(UInt32):
                    var bit32 = ReverseEndianness(Unsafe.As<T, UInt32>(ref value));
                    return Unsafe.As<UInt32, T>(ref bit32);
                case sizeof(UInt64):
                    var bit64 = ReverseEndianness(Unsafe.As<T, UInt64>(ref value));
                    return Unsafe.As<UInt64, T>(ref bit64);
                default:
                    throw new InvalidOperationException(UnableToReverseEndianness);
            }
        }

        internal static void ReverseEndianness<T>(T[] array) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(UInt16):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, UInt16>(ref array[i]) = ReverseEndianness(Unsafe.As<T, UInt16>(ref array[i]));
                    break;
                case sizeof(UInt32):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, UInt32>(ref array[i]) = ReverseEndianness(Unsafe.As<T, UInt32>(ref array[i]));
                    break;
                case sizeof(UInt64):
                    for (int i = 0; i < array.Length; i++)
                        Unsafe.As<T, UInt64>(ref array[i]) = ReverseEndianness(Unsafe.As<T, UInt64>(ref array[i]));
                    break;
                default:
                    throw new InvalidOperationException(UnableToReverseEndianness);
            }
        }

        internal static void ReverseEndiannessExplicitly<T>(byte[] buffer) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case sizeof(UInt16):
                    for (int i = 0; i < buffer.Length; i += sizeof(UInt16))
                        Unsafe.As<byte, UInt16>(ref buffer[i]) = ReverseEndianness(Unsafe.As<byte, UInt16>(ref buffer[i]));
                    break;
                case sizeof(UInt32):
                    for (int i = 0; i < buffer.Length; i += sizeof(UInt32))
                        Unsafe.As<byte, UInt32>(ref buffer[i]) = ReverseEndianness(Unsafe.As<byte, UInt32>(ref buffer[i]));
                    break;
                case sizeof(UInt64):
                    for (int i = 0; i < buffer.Length; i += sizeof(UInt64))
                        Unsafe.As<byte, UInt64>(ref buffer[i]) = ReverseEndianness(Unsafe.As<byte, UInt64>(ref buffer[i]));
                    break;
                default:
                    throw new InvalidOperationException(UnableToReverseEndianness);
            }
        }
    }
}
