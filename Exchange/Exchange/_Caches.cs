using System;
using System.Collections;
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
        internal const int _Length = 64;
        internal const int _Depth = 64;

        private static readonly MethodInfo s_getlist = typeof(_Element).GetMethod(nameof(_Element.List), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_getarray = typeof(_Element).GetMethod(nameof(_Element.Array), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly ConditionalWeakTable<Type, IPacketConverter> s_type = new ConditionalWeakTable<Type, IPacketConverter>();
        private static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_enum = new ConditionalWeakTable<Type, Func<PacketReader, object>>();

        private static readonly ConditionalWeakTable<Type, SolveInfo> s_solv = new ConditionalWeakTable<Type, SolveInfo>();
        private static readonly ConditionalWeakTable<Type, DissoInfo> s_anon = new ConditionalWeakTable<Type, DissoInfo>();

        private static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_list = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();
        private static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_array = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();

        private static IPacketConverter _BuildConverter(Type type)
        {
            if (type.IsEnum == false)
                return null;
            var und = Enum.GetUnderlyingType(type);
            if (s_cons.TryGetValue(und, out var res))
                return res;
            return null;
        }

        internal static object List(PacketReader reader, Type type)
        {
            var con = Converter(reader._cvt, type, false);
            if (s_list.TryGetValue(type, out var val) == false)
            {
                var met = s_getlist.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(ele, met, arg);
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
                var com = fun.Compile();
                val = s_list.GetValue(type, Wrap(com).Value);
            }
            return val.Invoke(reader._spa, con);
        }

        internal static object Array(PacketReader reader, Type type)
        {
            var con = Converter(reader._cvt, type, false);
            if (s_array.TryGetValue(type, out var val) == false)
            {
                var met = s_getarray.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(ele, met, arg);
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
                var com = fun.Compile();
                val = s_array.GetValue(type, Wrap(com).Value);
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
                val = s_enum.GetValue(type, Wrap(com).Value);
            }
            return val.Invoke(reader);
        }

        internal static SolveInfo GetMethods(Type type)
        {
            if (s_solv.TryGetValue(type, out var sol))
                return sol;

            var inf = new List<Info>();
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
                inf.Add(new Info { name = cur.Name, type = cur.PropertyType });
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

            var res = new SolveInfo { func = del.Compile(), args = inf.ToArray() };
            return s_solv.GetValue(type, Wrap(res).Value);
        }

        private static DissoInfo _DissolveAnonymousType(Type type)
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
            var inf = new Info[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new Info { name = cur.Name, type = cur.ParameterType };
            }

            // Reference type
            var ins = Expression.New(con, arr);
            var del = Expression.Lambda<Func<object[], object>>(ins, ipt);
            var res = new DissoInfo { func = del.Compile(), args = inf };

            return s_anon.GetValue(type, Wrap(res).Value);
        }

        private static DissoInfo _DissolveType(Type type, ConstructorInfo constructor)
        {
            var pro = type.GetProperties();
            var ins = (constructor == null) ? Expression.New(type) : Expression.New(constructor);
            var inf = new List<Info>();
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
                inf.Add(new Info { name = cur.Name, type = cur.PropertyType });
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
                var cvt = Expression.Convert(idx, inf[i].type);
                var set = Expression.Call(val, met[i], cvt);
                exp.Add(set);
            }

            var cst = Expression.Convert(val, typeof(object));
            exp.Add(cst);

            var blk = Expression.Block(new[] { val }, exp);
            var del = Expression.Lambda<Func<object[], object>>(blk, ipt);

            var res = new DissoInfo { func = del.Compile(), args = inf.ToArray() };
            return s_anon.GetValue(type, Wrap(res).Value);
        }

        internal static DissoInfo SetMethods(Type type)
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

        internal static IPacketConverter Converter<T>(ConverterDictionary dic, bool nothrow)
        {
            return Converter(dic, typeof(T), nothrow);
        }

        internal static IPacketConverter Converter(ConverterDictionary dic, Type type, bool nothrow)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
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
            else return s_type.GetValue(type, Wrap(res).Value);

            fail:
            if (nothrow == true)
                return null;
            throw new PacketException(PacketError.InvalidType);
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value)
        {
            var con = Converter(dic, type, false);
            var buf = con._GetBytesWrapError(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary dic, T value)
        {
            var con = Converter<T>(dic, false);
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErrorGeneric(value);
            return con._GetBytesWrapError(value);
        }

        internal static byte[] GetBytes(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = Converter(dic, type, false);
            var mst = GetStream();
            mst._WriteEnumerable(con, itr);
            return mst.ToArray();
        }

        internal static byte[] GetBytesGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var typ = typeof(T);
            // sbyte[] is ICollection<byte> ??? WTF!
            if (typ == typeof(byte) && itr is ICollection<byte> byt)
                return byt._OfByteCollection();
            else if (typ == typeof(sbyte) && itr is ICollection<sbyte> sby)
                return sby._OfSByteCollection();

            var con = Converter<T>(dic, false);
            var mst = GetStream();
            mst._WriteEnumerableGeneric(con, itr);
            return mst.ToArray();
        }

        internal static byte[] GetBytes(IPacketConverter con, IEnumerable itr)
        {
            var mst = GetStream();
            mst._WriteEnumerable(con, itr);
            return mst.ToArray();
        }
    }
}
