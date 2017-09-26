using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;

namespace Mikodev.Network
{
    internal static class _Caches
    {
        internal static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_func = new ConditionalWeakTable<Type, Func<PacketReader, object>>();
        internal static readonly MethodInfo s_method = typeof(_Caches).GetMethod(nameof(_Pull), BindingFlags.Static | BindingFlags.NonPublic);

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
            return s_func.GetValue(type, _ => fun);
        }

        /// <summary>
        /// Thread safe method
        /// </summary>
        internal static IPacketConverter Converter(Type type, Dictionary<Type, IPacketConverter> dic, bool nothrow)
        {
            if (dic != null && dic.TryGetValue(type, out var value))
                return value;
            if (s_cons.TryGetValue(type, out value))
                return value;
            if (s_type.TryGetValue(type, out value))
                return value;

            var val = _Create(type);
            if (val != null)
                return s_type.GetValue(type, _ => val);
            if (nothrow == true)
                return null;
            throw new PacketException(PacketError.TypeInvalid);
        }
    }
}
