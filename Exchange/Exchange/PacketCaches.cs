using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Mikodev.Network.PacketExtensions;

namespace Mikodev.Network
{
    internal static class PacketCaches
    {
        private static ConditionalWeakTable<Type, PacketConverter> s_dic = new ConditionalWeakTable<Type, PacketConverter>();

        private static PacketConverter _Define(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                var und = Enum.GetUnderlyingType(type);
                if (s_Converters.TryGetValue(und, out var res))
                    return res;
                return null;
            }

            try
            {
                var len = Marshal.SizeOf(type);
                return new PacketConverter(
                    _GetBytes,
                    (a, i, l) => _GetValue(a, i, l, type),
                    len);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Thread safe method.
        /// </summary>
        public static bool TryGetValue(Type type, out PacketConverter value)
        {
            if (s_dic.TryGetValue(type, out value))
                return true;

            var val = _Define(type);
            value = (val != null)
                ? s_dic.GetValue(type, _ => val)
                : null;
            return val != null;
        }
    }
}
