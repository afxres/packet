using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static readonly MethodInfo s_from_enumerable = typeof(_Caches).GetMethod(nameof(_FromEnumerable), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_from_enumerable_key_value_pair = typeof(_Caches).GetMethod(nameof(_FromEnumerableKeyValuePair), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo s_cast_array = typeof(_Caches).GetMethod(nameof(CastToArray), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_cast_list = typeof(_Caches).GetMethod(nameof(CastToList), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo s_cast_dictionary = typeof(_Caches).GetMethod(nameof(CastToDictionary), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo s_get_array = typeof(_Element).GetMethod(nameof(_Element.Array), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_list = typeof(_Element).GetMethod(nameof(_Element.List), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_collection = typeof(_Element).GetMethod(nameof(_Element.Collection), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_enumerable = typeof(_Element).GetMethod(nameof(_Element.Enumerable), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_dictionary = typeof(_Element).GetMethod(nameof(_Element.Dictionary), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly ConcurrentDictionary<Type, _Inf> s_info = new ConcurrentDictionary<Type, _Inf>();
        private static readonly ConcurrentDictionary<Type, GetterInfo> s_getter = new ConcurrentDictionary<Type, GetterInfo>();
        private static readonly ConcurrentDictionary<Type, SetterInfo> s_setter = new ConcurrentDictionary<Type, SetterInfo>();

        private static _Inf _CreateInfo(Type type)
        {
            var one = default(Type);
            var inf = new _Inf();

            if (type.IsEnum)
            {
                inf.Flags |= _Inf.Enum;
                inf.ElementType = Enum.GetUnderlyingType(type);
                return inf;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var arg = type.GetGenericArguments();
                inf.Flags |= _Inf.KeyValuePair;
                inf.IndexType = arg[0];
                inf.ElementType = arg[1];
                return inf;
            }

            void _SetEnumerableKeyValuePair(Type index, Type element, _Inf enumerable)
            {
                inf.Flags |= _Inf.EnumerableKeyValuePair;
                inf.IndexType = index;
                inf.ElementType = element;
                inf.FromEnumerableKeyValuePair = enumerable.FromEnumerableKeyValuePair;
                inf.GetEnumerableKeyValuePairAdapter = enumerable.GetEnumerableKeyValuePairAdapter;
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                one = type.GetElementType();
                inf.ElementType = one;

                inf.Flags |= _Inf.Array;
                inf.GetArray = _CreateGetFunction(s_get_array, one);
                inf.CastToArray = _CreateCastFunction(s_cast_array, one);
            }
            else if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();
                var arg = type.GetGenericArguments();
                if (arg.Length == 1)
                {
                    one = arg[0];
                    inf.ElementType = one;
                    var fun = default(Func<_Element, IPacketConverter, object>);

                    if (def == typeof(IEnumerable<>))
                    {
                        inf.Flags |= _Inf.Enumerable;
                        inf.GetEnumerable = _CreateGetFunction(s_get_enumerable, one);
                        inf.CastToArray = _CreateCastFunction(s_cast_array, one);

                        // Create from ... function
                        var kvp = GetInfo(one);
                        if ((kvp.Flags & _Inf.KeyValuePair) != 0)
                        {
                            inf.FromEnumerableKeyValuePair = _CreateFromEnumerableKeyValuePairFunction(kvp.IndexType, kvp.ElementType);
                            inf.GetEnumerableKeyValuePairAdapter = _CreateGetAdapterFunction(kvp.IndexType, kvp.ElementType);
                        }
                        inf.FromEnumerable = _CreateFromEnumerableFunction(one);
                    }
                    else if (def == typeof(List<>) || def == typeof(IList<>))
                    {
                        inf.Flags |= _Inf.List;
                        inf.GetList = _CreateGetFunction(s_get_list, one);
                        inf.CastToList = _CreateCastFunction(s_cast_list, one);
                    }
                    else if ((fun = _CreateGetCollectionFunction(type, one, out var info)) != null)
                    {
                        inf.Flags |= _Inf.Collection;
                        inf.GetCollection = fun;
                        inf.CastToCollection = _CreateCastToCollectionFunction(one, info);
                    }
                }
                else if (arg.Length == 2)
                {
                    var kvp = typeof(KeyValuePair<,>).MakeGenericType(arg);
                    var itr = type.GetInterfaces()
                        .Select(r => GetInfo(r))
                        .Where(r => (r.Flags & _Inf.Enumerable) != 0 && r.ElementType == kvp)
                        .FirstOrDefault();

                    if (itr != null)
                        _SetEnumerableKeyValuePair(arg[0], arg[1], itr);

                    if (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>))
                    {
                        inf.Flags |= _Inf.Dictionary;
                        inf.GetDictionary = _CreateGetDictionaryFunction(arg[0], arg[1]);
                        inf.CastToDictionary = _CreateCastToDictionaryFunction(arg[0], arg[1]);
                    }
                }
            }

            var obj = type.GetInterfaces()
                .Select(r => GetInfo(r))
                .Where(r => (r.Flags & _Inf.Enumerable) != 0);
            if (one != null)
                obj = obj.Where(r => r.ElementType == one);
            var lst = obj.ToList();

            if (lst.Count == 1)
            {
                var itr = lst[0];
                var kvp = GetInfo(itr.ElementType);
                if ((kvp.Flags & _Inf.KeyValuePair) == 0)
                {
                    inf.Flags |= _Inf.EnumerableImpl;
                    inf.ElementType = itr.ElementType;
                    inf.FromEnumerable = itr.FromEnumerable;
                }
                else if ((inf.Flags & _Inf.EnumerableKeyValuePair) == 0)
                {
                    _SetEnumerableKeyValuePair(kvp.IndexType, kvp.ElementType, lst[0]);
                }
            }

            return inf;
        }

        internal static _Inf GetInfo(Type type)
        {
            if (s_info.TryGetValue(type, out var inf))
                return inf;
            return s_info.GetOrAdd(type, _CreateInfo(type));
        }

        private static Func<_Element, IPacketConverter, object> _CreateGetFunction(MethodInfo info, Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var ele = Expression.Parameter(typeof(_Element), "element");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(ele, met, con);
            var exp = Expression.Lambda<Func<_Element, IPacketConverter, object>>(cal, ele, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<_Element, IPacketConverter, object> _CreateGetCollectionFunction(Type type, Type element, out ConstructorInfo info)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var cto = type.GetConstructor(new[] { itr });
            info = cto;
            if (cto == null)
                return null;
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var ele = Expression.Parameter(typeof(_Element), "element");
            var met = s_get_collection.MakeGenericMethod(element);
            var cal = Expression.Call(ele, met, con);
            var cst = Expression.Convert(cal, itr);
            var inv = Expression.New(cto, cst);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<_Element, IPacketConverter, object>>(box, ele, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<_Element, IPacketConverter, IPacketConverter, object> _CreateGetDictionaryFunction(Type index, Type element)
        {
            var ele = Expression.Parameter(typeof(_Element), "element");
            var key = Expression.Parameter(typeof(IPacketConverter), "key");
            var val = Expression.Parameter(typeof(IPacketConverter), "value");
            var met = s_get_dictionary.MakeGenericMethod(index, element);
            var cal = Expression.Call(ele, met, key, val);
            var exp = Expression.Lambda<Func<_Element, IPacketConverter, IPacketConverter, object>>(cal, ele, key, val);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> _CreateCastFunction(MethodInfo info, Type element)
        {
            var arr = Expression.Parameter(typeof(object[]), "array");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<object[], object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> _CreateCastToCollectionFunction(Type element, ConstructorInfo info)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var arr = Expression.Parameter(typeof(object[]), "array");
            var cal = Expression.Call(s_cast_array.MakeGenericMethod(element), arr);
            var cst = Expression.Convert(cal, itr);
            var inv = Expression.New(info, cst);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IEnumerable<KeyValuePair<object, object>>, object> _CreateCastToDictionaryFunction(Type index, Type element)
        {
            var arr = Expression.Parameter(typeof(IEnumerable<KeyValuePair<object, object>>), "pairs");
            var met = s_cast_dictionary.MakeGenericMethod(index, element);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<IEnumerable<KeyValuePair<object, object>>, object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, MemoryStream> _CreateFromEnumerableFunction(Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var obj = Expression.Parameter(typeof(object), "object");
            var met = s_from_enumerable.MakeGenericMethod(element);
            var cal = Expression.Call(met, con, obj);
            var exp = Expression.Lambda<Func<IPacketConverter, object, MemoryStream>>(cal, con, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, IPacketConverter, object, MemoryStream> _CreateFromEnumerableKeyValuePairFunction(Type index, Type element)
        {
            var key = Expression.Parameter(typeof(IPacketConverter), "index");
            var val = Expression.Parameter(typeof(IPacketConverter), "element");
            var obj = Expression.Parameter(typeof(object), "object");
            var met = s_from_enumerable_key_value_pair.MakeGenericMethod(index, element);
            var cal = Expression.Call(met, key, val, obj);
            var exp = Expression.Lambda<Func<IPacketConverter, IPacketConverter, object, MemoryStream>>(cal, key, val, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> _CreateGetAdapterFunction(Type index, Type element)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(index, element));
            var cto = typeof(_EnumerableAdapter<,>).MakeGenericType(index, element).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            var key = Expression.Parameter(typeof(IPacketConverter), "index");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, itr);
            var inv = Expression.New(cto, key, cvt);
            var exp = Expression.Lambda<Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>>>(inv, key, obj);
            var fun = exp.Compile();
            return fun;
        }

        internal static MemoryStream _FromEnumerable<T>(IPacketConverter converter, object @object)
        {
            if (converter is IPacketConverter<T> gen)
                return _StreamFromEnumerableGeneric(gen, (IEnumerable<T>)@object);
            else return _StreamFromEnumerable(converter, (IEnumerable)@object);
        }

        internal static MemoryStream _FromEnumerableKeyValuePair<TK, TV>(IPacketConverter key, IPacketConverter value, object @object)
        {
            return _GetStreamGeneric(key, value, (IEnumerable<KeyValuePair<TK, TV>>)@object);
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

            var res = new GetterInfo(inf.ToArray(), del.Compile());
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

        private static MemoryStream _StreamFromEnumerable(IPacketConverter con, IEnumerable itr)
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

        private static MemoryStream _StreamFromEnumerableGeneric<T>(IPacketConverter<T> con, IEnumerable<T> itr)
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

        private static MemoryStream _GetStreamGeneric<TK, TV>(IPacketConverter keycon, IPacketConverter valcon, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var mst = new MemoryStream(_Length);
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;
            var keylen = keycon.Length;
            var vallen = valcon.Length;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var keybuf = (keygen != null ? keygen._GetBytesWrapErrorGeneric(key) : keycon._GetBytesWrapError(key));
                if (keylen > 0)
                {
                    mst.Write(keybuf, 0, keylen);
                }
                else
                {
                    var len = keybuf.Length;
                    var pre = (len == 0 ? s_zero_bytes : BitConverter.GetBytes(len));
                    mst.Write(pre, 0, sizeof(int));
                    mst.Write(keybuf, 0, len);
                }

                var val = i.Value;
                var valbuf = (valgen != null ? valgen._GetBytesWrapErrorGeneric(val) : valcon._GetBytesWrapError(val));
                if (vallen > 0)
                {
                    mst.Write(valbuf, 0, vallen);
                }
                else
                {
                    var len = valbuf.Length;
                    var pre = (len == 0 ? s_zero_bytes : BitConverter.GetBytes(len));
                    mst.Write(pre, 0, sizeof(int));
                    mst.Write(valbuf, 0, len);
                }
            }

            return mst;
        }

        internal static MemoryStream GetStream(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = GetConverter(dic, type, false);
            var mst = _StreamFromEnumerable(con, itr);
            return mst;
        }

        internal static MemoryStream GetStreamGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> gen)
                return _StreamFromEnumerableGeneric(gen, itr);
            return _StreamFromEnumerable(con, itr);
        }

        internal static MemoryStream GetStreamGeneric<TK, TV>(ConverterDictionary dic, IEnumerable<KeyValuePair<TK, TV>> itr)
        {
            var key = GetConverter<TK>(dic, false);
            var val = GetConverter<TV>(dic, false);
            var mst = _GetStreamGeneric(key, val, itr);
            return mst;
        }

        internal static T[] CastToArray<T>(object[] array)
        {
            var length = array.Length;
            var result = new T[length];
            Array.Copy(array, result, length);
            return result;
        }

        internal static List<T> CastToList<T>(object[] array)
        {
            var values = CastToArray<T>(array);
            var result = new List<T>(values);
            return result;
        }

        internal static Dictionary<TK, TV> CastToDictionary<TK, TV>(IEnumerable<KeyValuePair<object, object>> values)
        {
            var dictionary = new Dictionary<TK, TV>();
            foreach (var i in values)
                dictionary.Add((TK)i.Key, (TV)i.Value);
            return dictionary;
        }

        internal static void _ClearCache()
        {
            s_info.Clear();
            s_getter.Clear();
            s_setter.Clear();
        }
    }
}
