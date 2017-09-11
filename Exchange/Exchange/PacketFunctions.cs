using System;
using System.Net;
using System.Runtime.InteropServices;
using static System.BitConverter;

namespace Mikodev.Network
{
    public static partial class PacketExtensions
    {
        internal static unsafe byte[] _OfValue(object str)
        {
            var len = Marshal.SizeOf(str);
            var buf = new byte[len];
            fixed (byte* ptr = buf)
                Marshal.StructureToPtr(str, (IntPtr)ptr, true);
            return buf;
        }

        internal static unsafe object _ToValue(byte[] buffer, int offset, int length, Type type)
        {
            var len = Marshal.SizeOf(type);
            if (length < len)
                throw new PacketException(PacketError.Overflow);
            if (offset < 0 || buffer.Length - offset < len)
                throw new PacketException(PacketError.AssertFailed);
            fixed (byte* ptr = buffer)
                return Marshal.PtrToStructure((IntPtr)(ptr + offset), type);
        }

        internal static byte[] _OfEndPoint(IPEndPoint value)
        {
            var add = value.Address.GetAddressBytes();
            var pot = GetBytes((ushort)value.Port);
            var res = new byte[add.Length + pot.Length];
            Buffer.BlockCopy(add, 0, res, 0, add.Length);
            Buffer.BlockCopy(pot, 0, res, add.Length, pot.Length);
            return res;
        }

        internal static IPEndPoint _ToEndPoint(byte[] buffer, int offset, int length)
        {
            var add = new IPAddress(buffer._Part(offset, length - sizeof(ushort)));
            var pot = ToUInt16(buffer, offset + length - sizeof(ushort));
            return new IPEndPoint(add, pot);
        }

        internal static byte[] _OfDecimal(decimal value)
        {
            var arr = decimal.GetBits(value);
            var buf = new byte[sizeof(decimal)];
            for (int i = 0; i < arr.Length; i++)
                Buffer.BlockCopy(GetBytes(arr[i]), 0, buf, i * sizeof(int), sizeof(int));
            return buf;
        }

        internal static decimal _ToDecimal(byte[] buffer, int offset, int length)
        {
            var arr = new int[sizeof(decimal) / sizeof(int)];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = ToInt32(buffer, offset + i * sizeof(int));
            var val = new decimal(arr);
            return val;
        }
    }
}
