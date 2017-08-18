using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mikodev.Network
{
    internal class PacketCaches
    {
        private object _loc = new object();
        private Dictionary<Type, WeakReference<PacketConverter>> _dic = null;

        private static PacketCaches s_ins = null;

        private PacketConverter _Value(Type type)
        {
            if (_dic.TryGetValue(type, out var ele))
            {
                if (ele.TryGetTarget(out var val))
                    return val;
                var con = _Create(type);
                if (con == null)
                    throw new PacketException(PacketError.AssertFailed);
                ele.SetTarget(con);
                return con;
            }
            var res = _Create(type);
            if (res == null)
                return null;
            _dic.Add(type, new WeakReference<PacketConverter>(res));
            return res;
        }

        private PacketConverter _Create(Type type)
        {
            if (type.GetTypeInfo().IsEnum == true)
            {
                var src = Enum.GetUnderlyingType(type);
                return new PacketConverter(
                    (obj) => Convert.ChangeType(obj, src)._GetBytes(),
                    (buf, off, len) => buf._GetValue(off, len, src),
                    Marshal.SizeOf(src));
            }
            else if (type.GetTypeInfo().IsValueType == true && type.GetTypeInfo().IsGenericType == false)
            {
                return new PacketConverter(
                    (obj) => obj._GetBytes(),
                    (buf, off, len) => buf._GetValue(off, len, type),
                    Marshal.SizeOf(type));
            }
            return null;
        }

        private static PacketCaches _Instance()
        {
            if (s_ins != null)
                return s_ins;
            Interlocked.CompareExchange(ref s_ins, new PacketCaches(), null);
            lock (s_ins._loc)
            {
                if (s_ins._dic == null)
                {
                    s_ins._dic = new Dictionary<Type, WeakReference<PacketConverter>>();
                }
            }
            return s_ins;
        }

        public static bool TryGetValue(Type type, out PacketConverter value)
        {
            var ins = _Instance();
            var res = default(PacketConverter);
            lock (ins._loc)
            {
                res = ins._Value(type);
            }
            value = res;
            return res != null;
        }
    }
}
