using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using PullFunc = System.Func<byte[], int, int, object>;
using PushFunc = System.Func<object, byte[]>;

namespace Mikodev.Network
{
    public static partial class PacketExtensions
    {
        internal static byte[] GetBytes<T>(this T str) where T : struct => GetBytes(str, typeof(T));

        internal static byte[] GetBytes(this object str, Type type)
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

        internal static T GetValue<T>(this byte[] buffer, int offset, int length) => (T)GetValue(buffer, offset, length, typeof(T));

        internal static object GetValue(this byte[] buffer, int offset, int length, Type type)
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

        internal static byte[] EndPointToBytes(IPEndPoint value)
        {
            var add = value.Address.GetAddressBytes();
            var pot = ((ushort)value.Port).GetBytes();
            return add.Merge(pot);
        }

        internal static IPEndPoint BytesToEndPoint(byte[] buffer, int offset, int length)
        {
            var len = sizeof(ushort);
            var add = new IPAddress(buffer.Split(offset, length - len));
            var pot = buffer.GetValue<short>(offset + length - len, len);
            return new IPEndPoint(add, pot);
        }

        internal static readonly Dictionary<Type, PushFunc> _PushDictionary = new Dictionary<Type, PushFunc>
        {
            [typeof(byte[])] = obj => (byte[])obj,
            [typeof(string)] = obj => Encoding.UTF8.GetBytes((string)obj),
            [typeof(DateTime)] = obj => ((DateTime)obj).ToBinary().GetBytes(),
            [typeof(IPAddress)] = obj => ((IPAddress)obj).GetAddressBytes(),
            [typeof(IPEndPoint)] = obj => EndPointToBytes((IPEndPoint)obj),
        };

        internal static readonly Dictionary<Type, PullFunc> _PullDictionary = new Dictionary<Type, PullFunc>
        {
            [typeof(byte[])] = Split,
            [typeof(string)] = Encoding.UTF8.GetString,
            [typeof(DateTime)] = (buf, off, len) => DateTime.FromBinary(buf.GetValue<long>(off, len)),
            [typeof(IPAddress)] = (buf, off, len) => new IPAddress(buf.Split(off, len)),
            [typeof(IPEndPoint)] = BytesToEndPoint,
        };
    }
}
