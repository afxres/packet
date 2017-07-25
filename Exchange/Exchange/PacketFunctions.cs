using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Mikodev.Network
{
    public static partial class PacketExtensions
    {
        internal static byte[] _GetBytes<T>(this T str) where T : struct => _GetBytes(str, typeof(T));

        internal static byte[] _GetBytes(this object str, Type type)
        {
            var len = Marshal.SizeOf(type);
            var buf = new byte[len];
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(len);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, buf, 0, len);
                return buf;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        internal static T _GetValue<T>(this byte[] buffer, int offset, int length) => (T)_GetValue(buffer, offset, length, typeof(T));

        internal static object _GetValue(this byte[] buffer, int offset, int length, Type type)
        {
            var len = Marshal.SizeOf(type);
            if (len > length)
                throw new PacketException(PacketError.Overflow);
            var ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(len);
                Marshal.Copy(buffer, offset, ptr, len);
                return Marshal.PtrToStructure(ptr, type);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        internal static byte[] _EndPointToBinary(IPEndPoint value)
        {
            var add = value.Address.GetAddressBytes();
            var pot = ((ushort)value.Port)._GetBytes();
            return add._Merge(pot);
        }

        internal static IPEndPoint _BinaryToEndPoint(byte[] buffer, int offset, int length)
        {
            var len = sizeof(ushort);
            var add = new IPAddress(buffer._Split(offset, length - len));
            var pot = buffer._GetValue<ushort>(offset + length - len, len);
            return new IPEndPoint(add, pot);
        }
    }
}
