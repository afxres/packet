using System.Runtime.CompilerServices;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Network
{
    internal static class Endian
    {
        #region basic swap methods
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
        #endregion

        #region swap single value
        internal static unsafe void Swap<T>(byte* target, byte* source) where T : unmanaged
        {
            switch (sizeof(T))
            {
                case sizeof(uint16):
                    *(uint16*)target = Swap16(*(uint16*)source);
                    break;
                case sizeof(uint32):
                    *(uint32*)target = Swap32(*(uint32*)source);
                    break;
                case sizeof(uint64):
                    *(uint64*)target = Swap64(*(uint64*)source);
                    break;
                default:
                    throw new System.ApplicationException();
            }
        }
        #endregion

        #region swap multiple values
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void SwapCopy<Item>(ref byte target, in Item source, int byteCount) where Item : unmanaged
        {
            fixed (byte* dst = &target)
            fixed (Item* src = &source)
                SwapCopy<Item>(dst, (byte*)src, byteCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void SwapCopy<Item>(ref Item target, in byte source, int byteCount) where Item : unmanaged
        {
            fixed (Item* dst = &target)
            fixed (byte* src = &source)
                SwapCopy<Item>((byte*)dst, src, byteCount);
        }

        internal static unsafe void SwapCopy<T>(byte* target, byte* source, int length) where T : unmanaged
        {
            switch (sizeof(T))
            {
                case sizeof(uint16):
                    for (var i = 0; i < length; i += sizeof(uint16))
                        *(uint16*)(target + i) = Swap16(*(uint16*)(source + i));
                    break;
                case sizeof(uint32):
                    for (var i = 0; i < length; i += sizeof(uint32))
                        *(uint32*)(target + i) = Swap32(*(uint32*)(source + i));
                    break;
                case sizeof(uint64):
                    for (var i = 0; i < length; i += sizeof(uint64))
                        *(uint64*)(target + i) = Swap64(*(uint64*)(source + i));
                    break;
                default:
                    throw new System.ApplicationException();
            }
        }
        #endregion
    }
}
