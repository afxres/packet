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
        /// <summary>
        /// 判断类型是否为值类型
        /// </summary>
        internal static bool IsValueType(this Type type) => type.GetTypeInfo().IsValueType;

        /// <summary>
        /// 合并多个字节数组
        /// </summary>
        internal static byte[] Merge(this byte[] buffer, params byte[][] values)
        {
            var str = new MemoryStream();
            str.Write(buffer, 0, buffer.Length);
            foreach (var v in values)
                str.Write(v, 0, v.Length);
            return str.ToArray();
        }

        /// <summary>
        /// 分割出字节数组中的特定部分
        /// </summary>
        internal static byte[] Split(this byte[] buffer, int offset, int length)
        {
            // 防止内存溢出
            if (length > buffer.Length || offset + length > buffer.Length)
                throw new PacketException(PacketErrorCode.LengthOverflow);
            var buf = new byte[length];
            Array.Copy(buffer, offset, buf, 0, length);
            return buf;
        }

        /// <summary>
        /// 将字节数组写入流中
        /// </summary>
        internal static void Write(this Stream stream, byte[] buffer, bool withLengthInfo = false)
        {
            if (withLengthInfo)
            {
                var len = BitConverter.GetBytes(buffer.Length);
                stream.Write(len, 0, len.Length);
            }
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 先将结构体转换成字节数组 然后写入流中
        /// </summary>
        internal static void Write<T>(this Stream stream, T value, bool withLengthInfo = false) where T : struct => stream.Write(value.GetBytes(typeof(T)), withLengthInfo);

        internal static byte[] TryRead(this Stream stream, int length)
        {
            if (length < 0)
                return null;
            var len = stream.Length;
            var cur = stream.Position;
            if (length > len - cur)
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

        /// <summary>
        /// 使用非托管内存将对象转化为字节数组 (仅针对结构体)
        /// </summary>
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

        /// <summary>
        /// 使用非托管内存将字节数组转换成结构体
        /// </summary>
        internal static object GetValue(this byte[] buffer, int offset, int length, Type type)
        {
            var len = Marshal.SizeOf(type);
            if (len > length)
                throw new PacketException(PacketErrorCode.LengthOverflow);
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
        /// </summary>
        public static Dictionary<Type, PushFunc> PushFuncs()
        {
            var dic = new Dictionary<Type, PushFunc>();
            dic.Add(typeof(string), (obj) => GetBytes((string)obj));
            dic.Add(typeof(DateTime), (obj) => GetBytes((DateTime)obj));
            dic.Add(typeof(IPAddress), (obj) => GetBytes((IPAddress)obj));
            dic.Add(typeof(IPEndPoint), (obj) => GetBytes((IPEndPoint)obj));
            return dic;
        }

        /// <summary>
        /// 默认的对象读取转换工具词典
        /// </summary>
        public static Dictionary<Type, PullFunc> PullFuncs()
        {
            var dic = new Dictionary<Type, PullFunc>();
            dic.Add(typeof(string), GetString);
            dic.Add(typeof(DateTime), (a, b, c) => GetDateTime(a, b, c));
            dic.Add(typeof(IPAddress), GetIPAddress);
            dic.Add(typeof(IPEndPoint), GetIPEndPoint);
            return dic;
        }

        /// <summary>
        /// 默认的路径分隔符
        /// </summary>
        /// <returns></returns>
        public static string[] GetSeparator() => new string[] { @"\", "/" };

        internal static byte[] GetBytes(this string str) => Encoding.UTF8.GetBytes(str);

        internal static string GetString(this byte[] buffer, int offset, int length) => Encoding.UTF8.GetString(buffer, offset, length);

        internal static byte[] GetBytes(this DateTime value) => value.ToBinary().GetBytes(typeof(long));

        internal static DateTime GetDateTime(this byte[] buffer, int offset, int length) => DateTime.FromBinary((long)buffer.GetValue(offset, length, typeof(long)));

        internal static byte[] GetBytes(this IPAddress value) => value.GetAddressBytes();

        internal static IPAddress GetIPAddress(this byte[] buffer, int offset, int length) => new IPAddress(buffer.Split(offset, length));

        internal static byte[] GetBytes(this IPEndPoint value) => value.Address.GetAddressBytes().Merge(((ushort)value.Port).GetBytes(typeof(ushort)));

        internal static IPEndPoint GetIPEndPoint(this byte[] buffer, int offset, int length)
        {
            var len = sizeof(ushort);
            var add = new IPAddress(buffer.Split(offset, length - len));
            var pot = (ushort)buffer.GetValue(offset + length - len, len, typeof(ushort));
            return new IPEndPoint(add, pot);
        }
    }
}
