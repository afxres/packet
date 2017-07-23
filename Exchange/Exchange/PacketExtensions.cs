using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using PullFunc = System.Func<byte[], int, int, object>;
using PushFunc = System.Func<object, byte[]>;

namespace Mikodev.Network
{
    /// <summary>
    /// 扩展方法模块
    /// </summary>
    public static partial class PacketExtensions
    {
        internal static readonly Dictionary<Type, int> _LengthDictionary = new Dictionary<Type, int>()
        {
            [typeof(DateTime)] = sizeof(long),
        };

        internal static readonly string[] _Separators = new string[] { "/", @"\" };

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

        internal static int GetLength(this Type type)
        {
            if (_LengthDictionary.TryGetValue(type, out var len))
                return len;
            return Marshal.SizeOf(type);
        }

        /// <summary>
        /// 默认的路径分隔符
        /// <para>Default path separators</para>
        /// </summary>
        public static IReadOnlyList<string> GetSeparators() => new string[] { @"\", "/" };

        /// <summary>
        /// 默认的对象写入转换工具词典
        /// <para>Default type converters (object -> byte array)</para>
        /// </summary>
        public static IReadOnlyDictionary<Type, PushFunc> GetPushConverters() => _PushDictionary;

        /// <summary>
        /// 默认的对象读取转换工具词典
        /// <para>Default type converters (byte array -> object)</para>
        /// </summary>
        public static IReadOnlyDictionary<Type, PullFunc> GetPullConverters() => _PullDictionary;
    }
}
