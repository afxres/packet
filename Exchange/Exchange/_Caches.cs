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

        internal static readonly ConditionalWeakTable<Type, _SolveInfo> s_solv = new ConditionalWeakTable<Type, _SolveInfo>();
        internal static readonly ConditionalWeakTable<Type, _DissoInfo> s_anon = new ConditionalWeakTable<Type, _DissoInfo>();

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
            var res = element._BuildCollection<T>(con);
            if (res == null)
                return new List<T>();
            if (res is T[] arr)
                return new List<T>(arr);
            if (res is List<T> lst)
                return lst;
            throw new InvalidOperationException();
        }

        internal static T[] _Array<T>(_Element element, IPacketConverter con)
        {
            var res = element._BuildCollection<T>(con);
            if (res == null)
                return new T[0];
            if (res is T[] arr)
                return arr;
            if (res is List<T> lst)
                return lst.ToArray();
            throw new InvalidOperationException();
        }

        internal static object List(PacketReader reader, Type type)
        {
            var con = Converter(type, reader._con, false);
            if (s_list.TryGetValue(type, out var val) == false)
            {
                var met = s_getlist.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(met, ele, arg);
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
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
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
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
                var inv = Expression.New(cts[0], par);
                var fun = Expression.Lambda<Func<PacketReader, object>>(inv, par);
                var com = fun.Compile();
                val = s_enum.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader);
        }

        internal static _SolveInfo GetMethods(Type type) => _SolveType(type);

        internal static _SolveInfo _SolveType(Type type)
        {
            if (s_solv.TryGetValue(type, out var sol))
                return sol;

            var inf = new List<_Info>();
            var met = new List<MethodInfo>();
            var pro = type.GetProperties();
            for (int i = 0; i < pro.Length; i++)
            {
                var cur = pro[i];
                var get = cur.GetGetMethod();
                if (get == null)
                    continue;
                var arg = get.GetParameters();
                // Length != 0 -> indexer
                if (arg == null || arg.Length != 0)
                    continue;
                inf.Add(new _Info { _name = cur.Name, _type = cur.PropertyType });
                met.Add(get);
            }

            var exp = new List<Expression>();
            var ipt = Expression.Parameter(typeof(object), "parameter");
            var arr = Expression.Parameter(typeof(object[]), "array");
            var val = Expression.Variable(type, "value");
            var ass = Expression.Assign(val, Expression.Convert(ipt, type));
            exp.Add(ass);

            for (int i = 0; i < inf.Count; i++)
            {
                var idx = Expression.ArrayAccess(arr, Expression.Constant(i));
                var inv = Expression.Call(val, met[i]);
                var cvt = Expression.Convert(inv, typeof(object));
                var set = Expression.Assign(idx, cvt);
                exp.Add(set);
            }

            var blk = Expression.Block(new[] { val }, exp);
            var del = Expression.Lambda<Action<object, object[]>>(blk, ipt, arr);

            var res = new _SolveInfo { _func = del.Compile(), _args = inf.ToArray() };
            return s_solv.GetValue(type, _Wrap(res).Value);
        }

        internal static _DissoInfo _DissolveAnonymousType(Type type)
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

            var ipt = Expression.Parameter(typeof(object[]), "parameters");
            var arr = new Expression[arg.Length];
            var inf = new _Info[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new _Info { _name = cur.Name, _type = cur.ParameterType };
            }

            // Reference type
            var ins = Expression.New(con, arr);
            var del = Expression.Lambda<Func<object[], object>>(ins, ipt);
            var res = new _DissoInfo { _func = del.Compile(), _args = inf };

            return s_anon.GetValue(type, _Wrap(res).Value);
        }

        internal static _DissoInfo _DissolveType(Type type, ConstructorInfo constructor)
        {
            var pro = type.GetProperties();
            var ins = (constructor == null) ? Expression.New(type) : Expression.New(constructor);
            var inf = new List<_Info>();
            var met = new List<MethodInfo>();

            for (int i = 0; i < pro.Length; i++)
            {
                var cur = pro[i];
                var get = cur.GetGetMethod();
                var set = cur.GetSetMethod();
                if (get == null || set == null)
                    continue;
                var arg = set.GetParameters();
                if (arg == null || arg.Length != 1)
                    continue;
                inf.Add(new _Info { _name = cur.Name, _type = cur.PropertyType });
                met.Add(set);
            }

            var exp = new List<Expression>();
            var ipt = Expression.Parameter(typeof(object[]), "parameters");
            var val = Expression.Variable(type, "value");
            var ass = Expression.Assign(val, ins);
            exp.Add(ass);

            for (int i = 0; i < inf.Count; i++)
            {
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, inf[i]._type);
                var set = Expression.Call(val, met[i], cvt);
                exp.Add(set);
            }

            var lbl = Expression.Label(typeof(object), "result");
            var cst = Expression.Convert(val, typeof(object));
            var ret = Expression.Return(lbl, cst);
            exp.Add(ret);
            exp.Add(Expression.Label(lbl, Expression.Constant(null)));

            var blk = Expression.Block(new[] { val }, exp);
            var del = Expression.Lambda<Func<object[], object>>(blk, ipt);

            var res = new _DissoInfo { _func = del.Compile(), _args = inf.ToArray() };
            return s_anon.GetValue(type, _Wrap(res).Value);
        }

        internal static _DissoInfo SetMethods(Type type)
        {
            if (s_anon.TryGetValue(type, out var val))
                return val;

            if (type.IsValueType)
                return _DissolveType(type, null);
            var con = type.GetConstructor(Type.EmptyTypes);
            if (con != null)
                return _DissolveType(type, con);
            return _DissolveAnonymousType(type);
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
            var buf = con._GetBytesWrapError(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(ConverterDictionary dic, T value)
        {
            var con = Converter(typeof(T), dic, false);
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapError(value);
            return con._GetBytesWrapError(value);
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value, out bool pre)
        {
            var con = Converter(type, dic, false);
            pre = con.Length < 1;
            var buf = con._GetBytesWrapError(value);
            return buf;
        }

        internal static byte[] GetBytes<T>(ConverterDictionary dic, T value, out bool pre)
        {
            var con = Converter(typeof(T), dic, false);
            pre = con.Length < 1;
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapError(value);
            return con._GetBytesWrapError(value);
        }
    }
}
