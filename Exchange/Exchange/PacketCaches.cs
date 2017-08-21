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
                return s_Converters[Enum.GetUnderlyingType(type)];

            if (type.GetTypeInfo().IsValueType == false || type.GetTypeInfo().IsGenericType)
                return null;

            return new PacketConverter(
                _GetBytes,
                (buf, off, len) => _GetValue(buf, off, len, type),
                Marshal.SizeOf(type));
        }

        /// <summary>
        /// Thread safe method.
        /// </summary>
        public static bool TryGetValue(Type type, out PacketConverter value)
        {
            if (s_dic.TryGetValue(type, out value))
                return true;

            var val = _Define(type);
            if (val == null)
                value = null;
            else
                value = s_dic.GetValue(type, _ => val);
            return val != null;
        }
    }
}
