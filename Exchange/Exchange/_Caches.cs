using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int _StreamLength = 64;
        internal const int _RecursionDepth = 64;

        internal static readonly MethodInfo s_getlist = typeof(_Caches).GetMethod(nameof(_List), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly MethodInfo s_getarray = typeof(_Caches).GetMethod(nameof(_Array), BindingFlags.Static | BindingFlags.NonPublic);

        internal static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        internal static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_enum = new ConditionalWeakTable<Type, Func<PacketReader, object>>();
        internal static readonly ConditionalWeakTable<Type, _Get[]> s_gets = new ConditionalWeakTable<Type, _Get[]>();
        internal static readonly ConditionalWeakTable<Type, object> s_anon = new ConditionalWeakTable<Type, object>();

        internal static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_list = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();
        internal static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_array = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();

        internal static IPacketConverter _BuildConverter(Type type)
        {
            if (type.IsEnum == false)
                return null;
            var und = Enum.GetUnderlyingType(type);
            if (s_cons.TryGetValue(und, out var res))
                return res;
            return null;
        }

        internal static List<T> _List<T>(_Element element, IPacketConverter con)
        {
            var spa = new _Element(element);
            var lst = new List<T>();
            if (con is IPacketConverter<T> gen)
                while (spa._Over() == false)
                    lst.Add(spa._Next<T>(gen));
            else
                while (spa._Over() == false)
                    lst.Add((T)spa._Next(con));
            return lst;
        }

        internal static T[] _Array<T>(_Element element, IPacketConverter con) => _List<T>(element, con).ToArray();

        internal static object List(PacketReader reader, Type type)
        {
            var con = Converter(type, reader._con, false);
            if (s_list.TryGetValue(type, out var val) == false)
            {
                var met = s_getlist.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(met, ele, arg);
                var cvt = Expression.Convert(inv, typeof(object));
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(cvt, ele, arg);
                var com = fun.Compile();
                val = s_list.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader._spa, con);
        }

        internal static object Array(PacketReader reader, Type type)
        {
            var con = Converter(type, reader._con, false);
            if (s_array.TryGetValue(type, out var val) == false)
            {
                var met = s_getarray.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(met, ele, arg);
                var cvt = Expression.Convert(inv, typeof(object));
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(cvt, ele, arg);
                var com = fun.Compile();
                val = s_array.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader._spa, con);
        }

        internal static object Enumerable(PacketReader reader, Type type)
        {
            if (s_enum.TryGetValue(type, out var val) == false)
            {
                var typ = typeof(_Enumerable<>).MakeGenericType(type);
                var cts = typ.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

                var par = Expression.Parameter(typeof(PacketReader), "reader");
                var exp = Expression.New(cts[0], par);
                var cvt = Expression.Convert(exp, typeof(object));
                var fun = Expression.Lambda<Func<PacketReader, object>>(cvt, par);
                var com = fun.Compile();
                val = s_enum.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader);
        }

        internal static _Get[] GetMethods(Type type)
        {
            if (s_gets.TryGetValue(type, out var val))
                return val;

            var lst = new List<_Get>();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var i in pro)
            {
                var get = i.GetGetMethod();
                if (get == null)
                    continue;
                var arg = get.GetParameters();
                // Length != 0 -> indexer
                if (arg == null || arg.Length != 0)
                    continue;
                var src = i.DeclaringType;
                var dst = i.PropertyType;

                var ins = Expression.Parameter(typeof(object), "instance");
                var cvt = Expression.Convert(ins, src);
                var inv = Expression.Call(cvt, get);
                var box = Expression.Convert(inv, typeof(object));
                var fun = Expression.Lambda<Func<object, object>>(box, ins);
                lst.Add(new _Get { _name = i.Name, _func = fun.Compile() });
            }
            var arr = lst.ToArray();
            return s_gets.GetValue(type, _Wrap(arr).Value);
        }

        internal static object _DissolveAnonymousType(Type type)
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
            var inf = new _Anon[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new _Anon { _name = cur.Name, _type = cur.ParameterType };
            }

            var ins = Expression.New(con, arr);
            var exp = Expression.Lambda<Func<object[], object>>(ins, ipt);
            var res = new _AnonInfo { _func = exp.Compile(), _args = inf };

            return s_anon.GetValue(type, _Wrap(res).Value);
        }

        internal static object SetMethods(Type type)
        {
            if (s_anon.TryGetValue(type, out var val))
                return val;

            var con = type.GetConstructor(new Type[0]);
            if (con == null)
                return _DissolveAnonymousType(type);

            var lst = new List<_Set>();
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
                var src = i.DeclaringType;
                var dst = i.PropertyType;

                var ins = Expression.Parameter(typeof(object), "instance");
                var par = Expression.Parameter(typeof(object), "value");
                var cvt = Expression.Convert(ins, src);
                var cas = Expression.Convert(par, dst);
                var inv = Expression.Call(cvt, set, cas);
                var fun = Expression.Lambda<Action<object, object>>(inv, ins, par);
                lst.Add(new _Set { _name = i.Name, _type = dst, _func = fun.Compile() });
            }

            var obj = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(con), typeof(object)));
            var res = new _SetInfo { _func = obj.Compile(), _sets = lst.ToArray() };

            return s_anon.GetValue(type, _Wrap(res).Value);
        }

        internal static IPacketConverter Converter(Type type, ConverterDictionary dic, bool nothrow)
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
            throw new PacketException(PacketError.InvalidType);
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value)
        {
            var con = Converter(type, dic, false);
            var buf = con._GetBytesWrapErr(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(ConverterDictionary dic, T value)
        {
            var con = Converter(typeof(T), dic, false);
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErr(value);
            return con._GetBytesWrapErr(value);
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value, out bool pre)
        {
            var con = Converter(type, dic, false);
            pre = con.Length < 1;
            var buf = con._GetBytesWrapErr(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(ConverterDictionary dic, T value, out bool pre)
        {
            var con = Converter(typeof(T), dic, false);
            pre = con.Length < 1;
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErr(value);
            return con._GetBytesWrapErr(value);
        }
    }
}
