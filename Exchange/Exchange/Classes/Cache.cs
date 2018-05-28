using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Cache
    {
        internal const int Length = 256;

        private static readonly ConcurrentDictionary<Type, Info> Infos = new ConcurrentDictionary<Type, Info>();
        private static readonly ConcurrentDictionary<Type, GetInfo> GetInfos = new ConcurrentDictionary<Type, GetInfo>();
        private static readonly ConcurrentDictionary<Type, SetInfo> SetInfos = new ConcurrentDictionary<Type, SetInfo>();

        internal static void ClearCache()
        {
            Infos.Clear();
            GetInfos.Clear();
            SetInfos.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PacketConverter GetConverter<T>(ConverterDictionary converters, bool nothrow) => GetConverter(converters, typeof(T), nothrow);

        internal static PacketConverter GetConverter(ConverterDictionary converters, Type type, bool nothrow)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (converters != null && converters.TryGetValue(type, out var value))
                if (value == null)
                    goto fail;
                else return value;
            if (Extension.Converters.TryGetValue(type, out value))
                return value;

            var info = GetInfo(type);
            if (info.Flag == InfoFlags.Enum)
                return Extension.Converters[info.ElementType];

            fail:
            if (nothrow == true)
                return null;
            throw PacketException.InvalidType(type);
        }

        internal static Info GetConverterOrInfo(ConverterDictionary converters, Type type, out PacketConverter converter)
        {
            if (converters != null && converters.TryGetValue(type, out converter))
                return null;
            if (Extension.Converters.TryGetValue(type, out converter))
                return null;
            var info = GetInfo(type);
            if (info.Flag != InfoFlags.Enum)
                return info;
            converter = Extension.Converters[info.ElementType];
            return null;
        }

        #region get bytes
        internal static byte[] GetBytes(Type type, ConverterDictionary converters, object value)
        {
            var con = GetConverter(converters, type, false);
            var buf = con.GetBytesWrap(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary converters, T value)
        {
            var con = GetConverter<T>(converters, false);
            if (con is PacketConverter<T> res)
                return res.GetBytesWrap(value);
            return con.GetBytesWrap(value);
        }

        internal static byte[][] GetBytesFromEnumerableNonGeneric(PacketConverter converter, IEnumerable enumerable)
        {
            var lst = new List<byte[]>();
            foreach (var i in enumerable)
                lst.Add(converter.GetBytesWrap(i));
            return lst.ToArray();
        }
        #endregion
    }
}
