using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;
using TypeTools = System.Collections.Generic.IReadOnlyDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int _RecDeep = 64;
        internal const int _StrInit = 64;

        internal static readonly MethodInfo s_itr = typeof(_Caches).GetMethod(nameof(_BuildEnumerable), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly MethodInfo s_get = typeof(_Caches).GetMethod(nameof(_BuildGetMethodGeneric), BindingFlags.Static | BindingFlags.NonPublic);

        internal static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_enum = new ConditionalWeakTable<Type, Func<PacketReader, object>>();
        internal static readonly ConditionalWeakTable<Type, Dictionary<string, Func<object, object>>> s_gets = new ConditionalWeakTable<Type, Dictionary<string, Func<object, object>>>();
        internal static readonly ConditionalWeakTable<Type, _AnonInfo> s_anon = new ConditionalWeakTable<Type, _AnonInfo>();

        internal static IEnumerable<T> _BuildEnumerable<T>(PacketReader source) => new _Enumerable<T>(source);

        internal static Func<object, object> _BuildGetMethodGeneric<T, R>(MethodInfo inf)
        {
            var del = Delegate.CreateDelegate(typeof(Func<T, R>), inf);
            var box = _Emit((Func<T, R>)del);
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
            if (s_enum.TryGetValue(type, out var val))
                return val;
            var fun = Delegate.CreateDelegate(typeof(Func<PacketReader, object>), s_itr.MakeGenericMethod(type));
            return s_enum.GetValue(type, _Wrap((Func<PacketReader, object>)fun).Value);
        }

        internal static Dictionary<string, Func<object, object>> GetMethods(Type type)
        {
            if (s_gets.TryGetValue(type, out var val))
                return val;
            var dic = new Dictionary<string, Func<object, object>>();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var i in pro)
            {
                var met = i.GetGetMethod();
                if (met == null)
                    continue;
                var arg = met.GetParameters();
                if (arg == null || arg.Length != 0)
                    continue;
                var src = i.DeclaringType;
                var dst = i.PropertyType;
                var res = s_get.MakeGenericMethod(src, dst).Invoke(null, new[] { met });
                dic.Add(i.Name, (Func<object, object>)res);
            }
            return s_gets.GetValue(type, _Wrap(dic).Value);
        }

        internal static _AnonInfo _DissolveAnonymousType(Type type)
        {
            var cts = type.GetConstructors();
            if (cts.Length != 1)
                return null;

            var con = cts[0];
            var arg = con.GetParameters();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (pro.Length != arg.Length)
                return null;

            for (int i = 0; i < pro.Length; i++)
                if (pro[i].Name != arg[i].Name || pro[i].PropertyType != arg[i].ParameterType)
                    return null;

            var ipt = Expression.Parameter(typeof(object[]), "inputs");
            var arr = new Expression[arg.Length];
            var inf = new _KeyValue<string, Type>[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new _KeyValue<string, Type> { _key = cur.Name, _value = cur.ParameterType };
            }

            var ins = Expression.New(con, arr);
            var exp = Expression.Lambda<Func<object[], object>>(ins, ipt);
            var fun = exp.Compile();
            var res = new _AnonInfo { _func = fun, _args = inf };

            return s_anon.GetValue(type, _Wrap(res).Value);
        }

        internal static _AnonInfo SetMethods(Type type)
        {
            if (s_anon.TryGetValue(type, out var val))
                return val;

            var con = type.GetConstructor(new Type[0]);
            if (con == null)
                return _DissolveAnonymousType(type);

            var lst = new List<_KeyValue<string, MethodInfo>>();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var i in pro)
            {
                var get = i.GetGetMethod();
                var set = i.GetSetMethod();
                if (get == null || set == null)
                    continue;
                var arg = set.GetParameters();
                if (arg == null || arg.Length != 1)
                    continue;
                lst.Add(new _KeyValue<string, MethodInfo> { _key = i.Name, _value = set });
            }


            var ipt = Expression.Parameter(typeof(object[]), "input");
            var arr = new Expression[lst.Count];
            var inf = new _KeyValue<string, Type>[lst.Count];
            for (int i = 0; i < lst.Count; i++)
            {
                var cur = lst[i];
            }
            return null;
        }

        internal static IPacketConverter Converter(Type type, TypeTools dic, bool nothrow)
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

        internal static byte[] GetBytes(Type type, TypeTools dic, object value)
        {
            var con = Converter(type, dic, false);
            var buf = con._GetBytesWrapErr(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(TypeTools dic, T value)
        {
            var con = Converter(typeof(T), dic, false);
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErr(value);
            return con._GetBytesWrapErr(value);
        }

        internal static byte[] GetBytes(Type type, TypeTools dic, object value, out bool pre)
        {
            var con = Converter(type, dic, false);
            pre = con.Length < 1;
            var buf = con._GetBytesWrapErr(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(TypeTools dic, T value, out bool pre)
        {
            var con = Converter(typeof(T), dic, false);
            pre = con.Length < 1;
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErr(value);
            return con._GetBytesWrapErr(value);
        }
    }
}
