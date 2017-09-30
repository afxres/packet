using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;

namespace Mikodev.Network
{
    internal static class _Caches
    {
        internal const int _RecDeep = 64;
        internal const int _StrInit = 64;

        internal static readonly MethodInfo s_method = typeof(_Caches).GetMethod(nameof(_Pull), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_func = new ConditionalWeakTable<Type, Func<PacketReader, object>>();

        internal static IEnumerable<T> _Pull<T>(PacketReader source) => new _Enumerable<T>(source);

        internal static IPacketConverter _Create(Type type)
        {
            if (type.IsEnum == false)
                return null;
            var und = Enum.GetUnderlyingType(type);
            if (s_cons.TryGetValue(und, out var res))
                return res;
            return null;
        }

        internal static Func<PacketReader, object> PullList(Type type)
        {
            if (s_func.TryGetValue(type, out var value))
                return value;
            var del = Delegate.CreateDelegate(typeof(Func<PacketReader, object>), s_method.MakeGenericMethod(type));
            var fun = (Func<PacketReader, object>)del;
            return s_func.GetValue(type, _Wrap(fun).Value);
        }

        internal static IPacketConverter Converter(Type type, IReadOnlyDictionary<Type, IPacketConverter> dic, bool nothrow)
        {
            if (dic != null && dic.TryGetValue(type, out var value))
                if (value == null)
                    goto fail;
                else return value;
            if (s_cons.TryGetValue(type, out value))
                return value;
            if (s_type.TryGetValue(type, out value))
                return value;

            var val = _Create(type);
            if (val == null)
                goto fail;
            else return s_type.GetValue(type, _Wrap(val).Value);

            fail:
            if (nothrow == true)
                return null;
            throw new PacketException(PacketError.TypeInvalid);
        }

        internal static byte[] GetBytes(Type type, IReadOnlyDictionary<Type, IPacketConverter> dic, object value, out bool pre)
        {
            var con = Converter(type, dic, false);
            pre = con.Length < 1;
            var buf = con._GetBytesWrapErr(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(IReadOnlyDictionary<Type, IPacketConverter> dic, T value, out bool pre)
        {
            var con = Converter(typeof(T), dic, false);
            pre = con.Length < 1;
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErr(value);
            return con._GetBytesWrapErr(value);
        }
    }
}
