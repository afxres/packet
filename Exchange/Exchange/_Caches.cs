using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int _RecDeep = 64;
        internal const int _StrInit = 256;

        internal static readonly MethodInfo s_itr = typeof(_Caches).GetMethod(nameof(_BuildEnumerable), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly MethodInfo s_get = typeof(_Caches).GetMethod(nameof(_BuildGetMethodGeneric), BindingFlags.Static | BindingFlags.NonPublic);

        internal static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_func = new ConditionalWeakTable<Type, Func<PacketReader, object>>();
        internal static readonly ConditionalWeakTable<Type, Dictionary<string, Func<object, object>>> s_prop = new ConditionalWeakTable<Type, Dictionary<string, Func<object, object>>>();

        internal static IEnumerable<T> _BuildEnumerable<T>(PacketReader source) => new _Enumerable<T>(source);

        internal static Func<object, object> _BuildGetMethod(PropertyInfo inf)
        {
            var met = inf.GetGetMethod();
            var src = inf.DeclaringType;
            var dst = inf.PropertyType;
            var res = s_get.MakeGenericMethod(src, dst).Invoke(null, new[] { met });
            return (Func<object, object>)res;
        }

        internal static Func<object, object> _BuildGetMethodGeneric<S, R>(MethodInfo inf)
        {
            var del = Delegate.CreateDelegate(typeof(Func<S, R>), inf);
            var box = _Emit((Func<S, R>)del);
            return box.Value;
        }

        internal static IPacketConverter _BuildConverter(Type type)
        {
            if (type.IsEnum == false)
                return null;
            var und = Enum.GetUnderlyingType(type);
            if (s_cons.TryGetValue(und, out var res))
                return res;
            return null;
        }

        internal static Func<PacketReader, object> Enumerable(Type type)
        {
            if (s_func.TryGetValue(type, out var val))
                return val;
            var fun = Delegate.CreateDelegate(typeof(Func<PacketReader, object>), s_itr.MakeGenericMethod(type));
            return s_func.GetValue(type, _Wrap((Func<PacketReader, object>)fun).Value);
        }

        internal static Dictionary<string, Func<object, object>> GetMethods(Type type)
        {
            if (s_prop.TryGetValue(type, out var val))
                return val;
            var dic = new Dictionary<string, Func<object, object>>();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var i in pro)
            {
                if (i.GetGetMethod() == null)
                    continue;
                dic.Add(i.Name, _BuildGetMethod(i));
            }
            return s_prop.GetValue(type, _Wrap(dic).Value);
        }

        internal static IPacketConverter Converter(Type type, IReadOnlyDictionary<Type, IPacketConverter> dic, bool nothrow)
        {
            if (dic != null && dic.TryGetValue(type, out var val))
                if (val == null)
                    goto fail;
                else return val;
            if (s_cons.TryGetValue(type, out val))
                return val;
            if (s_type.TryGetValue(type, out val))
                return val;

            var res = _BuildConverter(type);
            if (res == null)
                goto fail;
            else return s_type.GetValue(type, _Wrap(res).Value);

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
