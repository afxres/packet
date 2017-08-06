using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mikodev.Network
{
    internal class PacketCaches
    {
        internal object _loc = new object();
        internal Dictionary<Type, WeakReference<PacketConverter>> _dic = null;

        internal static PacketCaches s_ins = null;

        internal bool _GetConverter(Type type, out PacketConverter value)
        {
            lock (_loc)
            {
                if (_dic.TryGetValue(type, out var element))
                {
                    if (element.TryGetTarget(out value))
                        return true;
                    if (_Create(type, out value))
                    {
                        element.SetTarget(value);
                        return true;
                    }
                }
                else if (_Create(type, out value))
                {
                    _dic.Add(type, new WeakReference<PacketConverter>(value));
                    return true;
                }
                value = null;
                return false;
            }
        }

        internal bool _Create(Type type, out PacketConverter value)
        {
            if (type.GetTypeInfo().IsEnum == true)
            {
                var src = Enum.GetUnderlyingType(type);
                value = new PacketConverter(
                    (obj) => Convert.ChangeType(obj, src)._GetBytes(src),
                    (buf, off, len) => buf._GetValue(off, len, src),
                    Marshal.SizeOf(src));
                return true;
            }
            else if (type.GetTypeInfo().IsValueType == true && type.GetTypeInfo().IsGenericType == false)
            {
                value = new PacketConverter(
                    (obj) => obj._GetBytes(type),
                    (buf, off, len) => buf._GetValue(off, len, type),
                    Marshal.SizeOf(type));
                return true;
            }
            value = null;
            return false;
        }

        internal static PacketCaches _GetInstance()
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
    }
}
