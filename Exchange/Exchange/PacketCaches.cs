using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using static Mikodev.Network.PacketExtensions;

namespace Mikodev.Network
{
    internal class PacketCaches
    {
        private object _loc = new object();

        private Dictionary<Type, WeakReference<PacketConverter>> _dic = new Dictionary<Type, WeakReference<PacketConverter>>();

        private static PacketCaches s_ins = new PacketCaches();

        private PacketCaches() { }

        private PacketConverter _Value(Type type)
        {
            if (_dic.TryGetValue(type, out var ele))
            {
                if (ele.TryGetTarget(out var val))
                    return val;
                var con = _Define(type);
                if (con == null)
                    throw new PacketException(PacketError.AssertFailed);
                ele.SetTarget(con);
                return con;
            }
            var res = _Define(type);
            if (res == null)
                return null;
            _dic.Add(type, new WeakReference<PacketConverter>(res));
            return res;
        }

        private PacketConverter _Define(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
                return s_Converters[Enum.GetUnderlyingType(type)];

            if (type.GetTypeInfo().IsValueType == false || type.GetTypeInfo().IsGenericType)
                return null;

            var use = Marshal.SizeOf(type);
            return new PacketConverter(
                _GetBytes,
                (buf, off, len) => _GetValue(buf, off, len, type),
                use);
        }

        public static bool TryGetValue(Type type, out PacketConverter value)
        {
            var res = default(PacketConverter);
            lock (s_ins._loc)
            {
                res = s_ins._Value(type);
            }
            value = res;
            return res != null;
        }
    }
}
