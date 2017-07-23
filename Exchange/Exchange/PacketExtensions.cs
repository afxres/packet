using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PullFunc = System.Func<byte[], int, int, object>;
using PushFunc = System.Func<object, byte[]>;

namespace Mikodev.Network
{
    /// <summary>
    /// 扩展方法模块
    /// </summary>
    public static partial class PacketExtensions
    {
        internal static bool IsValueType(this Type type) => type.GetTypeInfo().IsValueType;

        internal static bool IsGenericEnumerable(this Type type) => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        internal static bool IsEnumerable(this Type typ, out Type inn)
        {
            foreach (var i in typ.GetTypeInfo().GetInterfaces())
            {
                if (i.IsGenericEnumerable())
                {
                    var som = i.GetGenericArguments();
                    if (som.Length != 1)
                        continue;
                    inn = som[0];
                    return true;
                }
            }
            inn = null;
            return false;
        }

        internal static byte[] Merge(this byte[] buffer, params byte[][] values)
        {
            var str = new MemoryStream();
            str.Write(buffer, 0, buffer.Length);
            foreach (var v in values)
                str.Write(v, 0, v.Length);
            return str.ToArray();
        }

        internal static byte[] Split(this byte[] buffer, int offset, int length)
        {
            if (length > buffer.Length)
                throw new PacketException(PacketError.Overflow);
            var buf = new byte[length];
            Array.Copy(buffer, offset, buf, 0, length);
            return buf;
        }

        internal static void Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void Write<T>(this Stream stream, T value) where T : struct => stream.Write(value.GetBytes());

        internal static void WriteExt(this Stream stream, byte[] buffer)
        {
            var len = BitConverter.GetBytes(buffer.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static byte[] TryRead(this Stream stream, int length)
        {
            if (length < 0 || stream.Position + length > stream.Length)
                return null;
            var buf = new byte[length];
            stream.Read(buf, 0, length);
            return buf;
        }

        internal static byte[] TryReadExt(this Stream stream)
        {
            var hdr = stream.TryRead(sizeof(int));
            if (hdr == null)
                return null;
            var len = BitConverter.ToInt32(hdr, 0);
            var res = stream.TryRead(len);
            return res;
        }

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

        /// <summary>
        /// 默认的对象写入转换工具词典
        /// <para>Default type converters (object -> byte array)</para>
        /// </summary>
        public static Dictionary<Type, PushFunc> PushFuncs()
        {
            var dic = new Dictionary<Type, PushFunc>
            {
                [typeof(byte[])] = obj => (byte[])obj,
                [typeof(string)] = obj => Encoding.UTF8.GetBytes((string)obj),
                [typeof(DateTime)] = obj => GetBytes((DateTime)obj),
                [typeof(IPAddress)] = obj => GetBytes((IPAddress)obj),
                [typeof(IPEndPoint)] = obj => GetBytes((IPEndPoint)obj),
            };
            return dic;
        }

        /// <summary>
        /// 默认的对象读取转换工具词典
        /// <para>Default type converters (byte array -> object)</para>
        /// </summary>
        public static Dictionary<Type, PullFunc> PullFuncs()
        {
            var dic = new Dictionary<Type, PullFunc>
            {
                [typeof(byte[])] = Split,
                [typeof(string)] = Encoding.UTF8.GetString,
                [typeof(DateTime)] = (a, b, c) => GetDateTime(a, b, c),
                [typeof(IPAddress)] = GetIPAddress,
                [typeof(IPEndPoint)] = GetIPEndPoint,
            };
            return dic;
        }

        /// <summary>
        /// 默认的路径分隔符
        /// <para>Default path separators</para>
        /// </summary>
        public static string[] GetSeparator() => new string[] { @"\", "/" };

        internal static byte[] GetBytes(this DateTime value) => value.ToBinary().GetBytes();

        internal static DateTime GetDateTime(this byte[] buffer, int offset, int length) => DateTime.FromBinary(buffer.GetValue<long>(offset, length));

        internal static byte[] GetBytes(this IPAddress value) => value.GetAddressBytes();

        internal static IPAddress GetIPAddress(this byte[] buffer, int offset, int length) => new IPAddress(buffer.Split(offset, length));

        internal static byte[] GetBytes(this IPEndPoint value) => value.Address.GetAddressBytes().Merge(((ushort)value.Port).GetBytes());

        internal static IPEndPoint GetIPEndPoint(this byte[] buffer, int offset, int length)
        {
            var len = sizeof(ushort);
            var add = new IPAddress(buffer.Split(offset, length - len));
            var pot = buffer.GetValue<short>(offset + length - len, len);
            return new IPEndPoint(add, pot);
        }
    }
}
