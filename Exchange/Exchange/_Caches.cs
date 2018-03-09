using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using static Mikodev.Network._Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int _Length = 256;
        internal const int _Depth = 64;

        private static readonly MethodInfo s_inf_array = typeof(_Element).GetMethod(nameof(_Element.Array), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_inf_list = typeof(_Element).GetMethod(nameof(_Element.List), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_inf_collection = typeof(_Caches).GetMethod(nameof(_GetCollection), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_inf_dictionary = typeof(_Caches).GetMethod(nameof(_GetDictionary), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_inf_sequence = typeof(_Caches).GetMethod(nameof(_GetSequenceAuto), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly ConcurrentDictionary<Type, GetterInfo> s_getter = new ConcurrentDictionary<Type, GetterInfo>();
        private static readonly ConcurrentDictionary<Type, SetterInfo> s_setter = new ConcurrentDictionary<Type, SetterInfo>();
        private static readonly ConcurrentDictionary<Type, _Inf> s_info = new ConcurrentDictionary<Type, _Inf>();

        private static readonly ConcurrentDictionary<Type, Func<_Element, IPacketConverter, object>> s_array = new ConcurrentDictionary<Type, Func<_Element, IPacketConverter, object>>();
        private static readonly ConcurrentDictionary<Type, Func<_Element, IPacketConverter, object>> s_list = new ConcurrentDictionary<Type, Func<_Element, IPacketConverter, object>>();
        private static readonly ConcurrentDictionary<Type, Func<PacketReader, IPacketConverter, object>> s_enumerable = new ConcurrentDictionary<Type, Func<PacketReader, IPacketConverter, object>>();
        private static readonly ConcurrentDictionary<Type, Func<IPacketConverter, object, MemoryStream>> s_bytes = new ConcurrentDictionary<Type, Func<IPacketConverter, object, MemoryStream>>();

        private static _Inf _CreateInfo(Type type)
        {
            var inf = new _Inf();
            var tag = 0;
            var one = false;

            if (type.IsEnum)
            {
                tag |= _Inf.Enum;
                inf.ElementType = Enum.GetUnderlyingType(type);
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                one = true;
                tag |= _Inf.Array;
                inf.ElementType = type.GetElementType();
            }

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                var arg = type.GetGenericArguments();
                if (arg.Length == 1)
                {
                    var fun = default(Func<PacketReader, object>);
                    if (def == typeof(IEnumerable<>))
                        tag |= _Inf.Enumerable;
                    else if (def == typeof(List<>) || def == typeof(IList<>))
                        tag |= _Inf.List;
                    else if ((fun = _CreateCollectionFunction(arg[0], type)) != null)
                        tag |= _Inf.Collection;
                    one = true;
                    inf.ElementType = arg[0];
                    inf.CollectionFunction = fun;
                }
                else if (arg.Length == 2 && (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>)))
                {
                    tag |= _Inf.Dictionary;
                    inf.IndexType = arg[0];
                    inf.ElementType = arg[1];
                    inf.DictionaryFunction = _CreateDictionaryFunction(arg[0], arg[1]);
                }
            }

            if (one == true)
            {
                foreach (var i in type.GetInterfaces())
                {
                    var det = GetInfo(i);
                    if ((det.Flags & _Inf.Enumerable) != 0 && det.ElementType == inf.ElementType)
                    {
                        tag |= _Inf.EnumerableImpl;
                        break;
                    }
                }
            }

            inf.Flags = tag;
            return inf;
        }

        internal static _Inf GetInfo(Type type)
        {
            if (s_info.TryGetValue(type, out var inf))
                return inf;
            return s_info.GetOrAdd(type, _CreateInfo(type));
        }

        private static Func<PacketReader, IPacketConverter, object> _CreateEnumerableFunction(Type type)
        {
            var typ = typeof(_Enumerable<>).MakeGenericType(type);
            var cts = typ.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            var src = Expression.Parameter(typeof(PacketReader), "reader");
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var inv = Expression.New(cts[0], src, con);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(inv, src, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<_Element, IPacketConverter, object> _CreateFunction(MethodInfo method, Type type)
        {
            var met = method.MakeGenericMethod(type);
            var ele = Expression.Parameter(typeof(_Element), "element");
            var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
            var inv = Expression.Call(ele, met, arg);
            var exp = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, object> _CreateCollectionFunction(Type element, Type type)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var cto = type.GetConstructor(new[] { itr });
            if (cto == null)
                return null;
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var cal = Expression.Call(s_inf_collection.MakeGenericMethod(element), rea);
            var inv = Expression.New(cto, cal);
            var exp = Expression.Lambda<Func<PacketReader, object>>(inv, rea);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, object> _CreateDictionaryFunction(Type index, Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = s_inf_dictionary.MakeGenericMethod(index, element);
            var cal = Expression.Call(met, rea);
            var exp = Expression.Lambda<Func<PacketReader, object>>(cal, rea);
            var fun = exp.Compile();
            return fun;
        }

        internal static object GetList(PacketReader reader, Type type)
        {
            var con = GetConverter(reader._converters, type, false);
            if (s_list.TryGetValue(type, out var fun) == false)
                fun = s_list.GetOrAdd(type, _CreateFunction(s_inf_list, type));
            var res = fun.Invoke(reader._element, con);
            return res;
        }

        internal static object GetArray(PacketReader reader, Type type)
        {
            var con = GetConverter(reader._converters, type, false);
            if (s_array.TryGetValue(type, out var fun) == false)
                fun = s_array.GetOrAdd(type, _CreateFunction(s_inf_array, type));
            var res = fun.Invoke(reader._element, con);
            return res;
        }

        internal static object GetEnumerable(PacketReader reader, Type type)
        {
            var con = GetConverter(reader._converters, type, false);
            if (s_enumerable.TryGetValue(type, out var fun) == false)
                fun = s_enumerable.GetOrAdd(type, _CreateEnumerableFunction(type));
            var res = fun.Invoke(reader, con);
            return res;
        }

        internal static Dictionary<TK, TV> _GetDictionary<TK, TV>(PacketReader reader)
        {
            var key = GetConverter(reader._converters, typeof(TK), false);
            var val = GetConverter(reader._converters, typeof(TV), false);
            var dic = reader._element.Dictionary<TK, TV>(key, val);
            return dic;
        }

        internal static IEnumerable<T> _GetCollection<T>(PacketReader reader)
        {
            var con = GetConverter(reader._converters, typeof(T), false);
            var val = reader._element.Collection<T>(con);
            return (IEnumerable<T>)val;
        }

        private static GetterInfo _CreateGetterInfo(Type type)
        {
            var inf = new List<AccessorInfo>();
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
                inf.Add(new AccessorInfo { Name = cur.Name, Type = cur.PropertyType });
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

            var res = new GetterInfo { Function = del.Compile(), Arguments = inf.ToArray() };
            return res;
        }

        private static SetterInfo _CreateSetterInfoForAnonymousType(Type type)
        {
            var res = new SetterInfo();
            var cts = type.GetConstructors();
            if (cts.Length != 1)
                return res;

            var con = cts[0];
            var arg = con.GetParameters();
            var pro = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (pro.Length != arg.Length)
                return res;

            for (int i = 0; i < pro.Length; i++)
                if (pro[i].Name != arg[i].Name || pro[i].PropertyType != arg[i].ParameterType)
                    return res;

            var ipt = Expression.Parameter(typeof(object[]), "parameters");
            var arr = new Expression[arg.Length];
            var inf = new AccessorInfo[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new AccessorInfo { Name = cur.Name, Type = cur.ParameterType };
            }

            // Reference type
            var ins = Expression.New(con, arr);
            var del = Expression.Lambda<Func<object[], object>>(ins, ipt);
            res.Function = del.Compile();
            res.Arguments = inf;
            return res;
        }

        private static SetterInfo _CreateSetterInfo(Type type, ConstructorInfo constructor)
        {
            var pro = type.GetProperties();
            var ins = (constructor == null) ? Expression.New(type) : Expression.New(constructor);
            var inf = new List<AccessorInfo>();
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
                inf.Add(new AccessorInfo { Name = cur.Name, Type = cur.PropertyType });
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
                var cvt = Expression.Convert(idx, inf[i].Type);
                var set = Expression.Call(val, met[i], cvt);
                exp.Add(set);
            }

            var cst = Expression.Convert(val, typeof(object));
            exp.Add(cst);

            var blk = Expression.Block(new[] { val }, exp);
            var del = Expression.Lambda<Func<object[], object>>(blk, ipt);

            var res = new SetterInfo { Function = del.Compile(), Arguments = inf.ToArray() };
            return res;
        }

        private static SetterInfo _CreateSetterInfo(Type type)
        {
            if (type.IsValueType)
                return _CreateSetterInfo(type, null);
            var con = type.GetConstructor(Type.EmptyTypes);
            if (con != null)
                return _CreateSetterInfo(type, con);
            return _CreateSetterInfoForAnonymousType(type);
        }

        internal static GetterInfo GetGetterInfo(Type type)
        {
            if (s_getter.TryGetValue(type, out var inf))
                return inf;
            return s_getter.GetOrAdd(type, _CreateGetterInfo(type));
        }

        internal static SetterInfo GetSetterInfo(Type type)
        {
            if (s_setter.TryGetValue(type, out var inf))
                return inf;
            return s_setter.GetOrAdd(type, _CreateSetterInfo(type));
        }

        internal static IPacketConverter GetConverter<T>(ConverterDictionary dic, bool nothrow)
        {
            return GetConverter(dic, typeof(T), nothrow);
        }

        internal static IPacketConverter GetConverter(ConverterDictionary dic, Type type, bool nothrow)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (dic != null && dic.TryGetValue(type, out var val))
                if (val == null)
                    goto fail;
                else return val;
            if (s_converters.TryGetValue(type, out val))
                return val;

            var det = GetInfo(type);
            if ((det.Flags & _Inf.Enum) != 0 && s_converters.TryGetValue(det.ElementType, out val))
                return val;

            fail:
            if (nothrow == true)
                return null;
            throw new PacketException(PacketError.InvalidType);
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value)
        {
            var con = GetConverter(dic, type, false);
            var buf = con._GetBytesWrapError(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary dic, T value)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> res)
                return res._GetBytesWrapErrorGeneric(value);
            return con._GetBytesWrapError(value);
        }

        private static MemoryStream _GetSequence(IPacketConverter con, IEnumerable itr)
        {
            var mst = new MemoryStream(_Length);
            var def = con.Length;
            if (def > 0)
            {
                foreach (var i in itr)
                {
                    mst.Write(con._GetBytesWrapError(i), 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapError(i);
                    var len = buf.Length;
                    var pre = (len == 0 ? s_zero_bytes : BitConverter.GetBytes(len));
                    mst.Write(pre, 0, sizeof(int));
                    mst.Write(buf, 0, len);
                }
            }
            return mst;
        }

        private static MemoryStream _GetSequenceGeneric<T>(IPacketConverter<T> con, IEnumerable<T> itr)
        {
            var mst = new MemoryStream(_Length);
            var def = con.Length;
            if (def > 0)
            {
                foreach (var i in itr)
                {
                    mst.Write(con._GetBytesWrapErrorGeneric(i), 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapErrorGeneric(i);
                    var len = buf.Length;
                    var pre = (len == 0 ? s_zero_bytes : BitConverter.GetBytes(len));
                    mst.Write(pre, 0, sizeof(int));
                    mst.Write(buf, 0, len);
                }
            }
            return mst;
        }

        private static MemoryStream _GetSequenceAuto<T>(IPacketConverter con, IEnumerable<T> itr)
        {
            if (con is IPacketConverter<T> gen)
                return _GetSequenceGeneric(gen, itr);
            return _GetSequence(con, itr);
        }

        internal static MemoryStream GetSequenceGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> gen)
                return _GetSequenceGeneric(gen, itr);
            return _GetSequence(con, itr);
        }

        internal static MemoryStream GetSequence(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = GetConverter(dic, type, false);
            var mst = _GetSequence(con, itr);
            return mst;
        }

        /// <summary>
        /// Return null if type invalid
        /// </summary>
        internal static MemoryStream GetSequenceReflection(ConverterDictionary dic, object itr, Type type)
        {
            var con = GetConverter(dic, type, true);
            if (con == null)
                return null;
            if (s_bytes.TryGetValue(type, out var fun) == false)
                fun = s_bytes.GetOrAdd(type, _CreateSequenceFunction(type));
            var seq = fun.Invoke(con, itr);
            return seq;
        }

        private static Func<IPacketConverter, object, MemoryStream> _CreateSequenceFunction(Type type)
        {
            var inf = s_inf_sequence.MakeGenericMethod(type);
            var cvt = Expression.Parameter(typeof(IPacketConverter), "converter");
            var enu = Expression.Parameter(typeof(object), "enumerable");
            var cst = Expression.TypeAs(enu, typeof(IEnumerable<>).MakeGenericType(type));
            var cal = Expression.Call(inf, cvt, cst);
            var exp = Expression.Lambda<Func<IPacketConverter, object, MemoryStream>>(cal, cvt, enu);
            var fun = exp.Compile();
            return fun;
        }

        internal static void _ClearCache()
        {
            s_info.Clear();
            s_getter.Clear();
            s_setter.Clear();

            s_array.Clear();
            s_list.Clear();
            s_enumerable.Clear();
            s_bytes.Clear();
        }
    }
}
