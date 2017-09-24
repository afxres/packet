using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Mikodev.Network.PacketExtensions;

namespace Mikodev.Network
{
    internal static class _Caches
    {
        internal static readonly ConditionalWeakTable<Type, PacketConverter> s_type = new ConditionalWeakTable<Type, PacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_func = new ConditionalWeakTable<Type, Func<PacketReader, object>>();
        internal static readonly MethodInfo s_method = typeof(PacketReader).GetMethods().First(r => r.Name == nameof(PacketReader.PullList) && r.IsGenericMethod);

        internal static PacketConverter _Create(Type type)
        {
            if (type.IsEnum)
            {
                var und = Enum.GetUnderlyingType(type);
                if (s_cons.TryGetValue(und, out var res))
                    return res;
                return null;
            }

            try
            {
                var len = Marshal.SizeOf(type);
                var con = new PacketConverter(_OfValue, (a, i, l) => _ToValue(a, i, l, type), len);
                return con;
            }
            catch (ArgumentException)
            {
                return null;
            }
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
        internal static PacketConverter Converter(Type type, Dictionary<Type, PacketConverter> dic, bool nothrow)
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
