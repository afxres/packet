using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Mikodev.Network._Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int Length = 256;
        internal const int Depth = 64;

        private static readonly ConcurrentDictionary<Type, _Inf> s_info = new ConcurrentDictionary<Type, _Inf>();
        private static readonly ConcurrentDictionary<Type, GetterInfo> s_getter = new ConcurrentDictionary<Type, GetterInfo>();
        private static readonly ConcurrentDictionary<Type, SetterInfo> s_setter = new ConcurrentDictionary<Type, SetterInfo>();

        internal static void ClearCache()
        {
            s_info.Clear();
            s_getter.Clear();
            s_setter.Clear();
        }

        private static void _GetInfoFromDictionary(_Inf inf, _Inf itr, params Type[] types)
        {
            if (types[0] == typeof(string) && types[1] == typeof(object))
                inf.From = _Inf.Map;
            else
                inf.From = _Inf.Dictionary;
            inf.IndexType = types[0];
            inf.ElementType = types[1];
            inf.FromDictionary = itr.FromDictionary;
            inf.FromDictionaryAdapter = itr.FromDictionaryAdapter;
        }

        private static _Inf _GetInfo(Type typ)
        {
            var inf = new _Inf() { Type = typ };
            if (typ.IsEnum)
            {
                inf.Flag = _Inf.Enum;
                inf.ElementType = Enum.GetUnderlyingType(typ);
                return inf;
            }

            var generic = typ.IsGenericType;
            var genericArgs = generic ? typ.GetGenericArguments() : null;
            var genericDefinition = generic ? typ.GetGenericTypeDefinition() : null;

            if (generic && typ.IsInterface)
            {
                if (genericDefinition == typeof(IEnumerable<>))
                {
                    var ele = genericArgs[0];
                    inf.ElementType = ele;
                    inf.To = _Inf.Enumerable;
                    inf.ToEnumerable = _GetToFunction(s_to_enumerable, ele);
                    inf.ToEnumerableAdapter = _GetToEnumerableAdapter(ele);

                    // From ... function
                    inf.From = _Inf.Enumerable;
                    inf.FromEnumerable = _GetFromEnumerableFunction(s_from_enumerable.MakeGenericMethod(ele), typ);
                    if (ele.IsGenericType && ele.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        var arg = ele.GetGenericArguments();
                        inf.FromDictionary = _GetFromDictionaryFunction(arg);
                        inf.FromDictionaryAdapter = _GetFromAdapterFunction(arg);
                    }
                    return inf;
                }
                if (genericDefinition == typeof(IList<>))
                {
                    var ele = genericArgs[0];
                    inf.ElementType = ele;
                    inf.To = _Inf.List;
                    inf.ToCollection = _GetToFunction(s_to_list, ele);
                    inf.ToCollectionCast = _GetCastFunction(s_cast_list, ele);
                    return inf;
                }
                if (genericDefinition == typeof(IDictionary<,>))
                {
                    var kvp = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
                    var itr = typeof(IEnumerable<>).MakeGenericType(kvp);
                    var sub = GetInfo(itr);
                    _GetInfoFromDictionary(inf, sub, genericArgs);
                    inf.To = _Inf.Dictionary;
                    inf.ToDictionary = _GetToDictionaryFunction(genericArgs);
                    inf.ToDictionaryCast = _GetCastDictionaryFunction(genericArgs);
                    return inf;
                }
            }

            if (typ.IsArray)
            {
                if (typ.GetArrayRank() != 1)
                    throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                var ele = typ.GetElementType();
                genericArgs = new Type[1] { ele };
                inf.ElementType = ele;

                inf.From = _Inf.Enumerable;
                inf.To = _Inf.Array;
                inf.FromEnumerable = _GetFromEnumerableFunction(s_from_array.MakeGenericMethod(ele), typ);
                inf.ToCollection = _GetToFunction(s_to_array, ele);
                inf.ToCollectionCast = _GetCastFunction(s_cast_array, ele);
            }
            else if (generic && genericArgs.Length == 1)
            {
                var ele = genericArgs[0];
                if (genericDefinition == typeof(List<>))
                {
                    var sub = GetInfo(typeof(IList<>).MakeGenericType(ele));
                    inf.ElementType = ele;
                    inf.From = _Inf.Enumerable;
                    inf.To = _Inf.List;
                    inf.FromEnumerable = _GetFromEnumerableFunction(s_from_list.MakeGenericMethod(ele), typ);
                    inf.ToCollection = sub.ToCollection;
                    inf.ToCollectionCast = sub.ToCollectionCast;
                }
                else
                {
                    var fun = _GetToCollectionFunction(typ, ele, out var ctor);
                    if (fun != null)
                    {
                        inf.ElementType = ele;
                        inf.To = _Inf.Collection;
                        inf.ToCollection = fun;
                        inf.ToCollectionCast = _GetCastCollectionFunction(ele, ctor);
                    }
                }
            }
            else if (generic && genericArgs.Length == 2)
            {
                if (genericDefinition == typeof(Dictionary<,>))
                {
                    inf.To = _Inf.Dictionary;
                    inf.ToDictionary = _GetToDictionaryFunction(genericArgs[0], genericArgs[1]);
                    inf.ToDictionaryCast = _GetCastDictionaryFunction(genericArgs[0], genericArgs[1]);
                }
            }

            var interfaces = typ.GetInterfaces();
            if (inf.From == _Inf.None)
            {
                if (interfaces.Contains(typeof(IDictionary<string, object>)))
                    inf.From = _Inf.Map;

                var lst = interfaces
                    .Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .Select(GetInfo)
                    .ToList();
                if (lst.Count > 1 && genericArgs != null)
                {
                    var cmp = default(Type);
                    if (genericArgs.Length == 1)
                        cmp = genericArgs[0];
                    else if (genericArgs.Length == 2)
                        cmp = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
                    if (cmp != null)
                        lst = lst.Where(r => r.ElementType == cmp).ToList();
                }

                if (lst.Count == 1)
                {
                    var itr = lst[0];
                    var ele = itr.ElementType;
                    if (ele.IsGenericType && ele.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        _GetInfoFromDictionary(inf, lst[0], ele.GetGenericArguments());
                    }
                    else
                    {
                        inf.From = _Inf.Enumerable;
                        inf.ElementType = itr.ElementType;
                        inf.FromEnumerable = itr.FromEnumerable;
                    }
                }
            }

            if (inf.From == _Inf.Enumerable)
            {
                if (inf.ElementType == typeof(byte) && interfaces.Contains(typeof(ICollection<byte>)))
                    inf.From = _Inf.Bytes;
                else if (inf.ElementType == typeof(sbyte) && interfaces.Contains(typeof(ICollection<sbyte>)))
                    inf.From = _Inf.SBytes;
            }
            return inf;
        }

        internal static _Inf GetInfo(Type type)
        {
            if (s_info.TryGetValue(type, out var inf))
                return inf;
            return s_info.GetOrAdd(type, _GetInfo(type));
        }

        private static GetterInfo _GetGetterInfo(Type type)
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

        private static SetterInfo _GetSetterInfoForAnonymousType(Type type)
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

        private static SetterInfo _GetSetterInfo(Type type, ConstructorInfo constructor)
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

        private static SetterInfo _GetSetterInfo(Type type)
        {
            if (type.IsValueType)
                return _GetSetterInfo(type, null);
            var con = type.GetConstructor(Type.EmptyTypes);
            if (con != null)
                return _GetSetterInfo(type, con);
            return _GetSetterInfoForAnonymousType(type);
        }

        internal static GetterInfo GetGetterInfo(Type type)
        {
            if (s_getter.TryGetValue(type, out var inf))
                return inf;
            return s_getter.GetOrAdd(type, _GetGetterInfo(type));
        }

        internal static SetterInfo GetSetterInfo(Type type)
        {
            if (s_setter.TryGetValue(type, out var inf))
                return inf;
            return s_setter.GetOrAdd(type, _GetSetterInfo(type));
        }

        internal static IPacketConverter GetConverter<T>(ConverterDictionary dic, bool nothrow)
        {
            return GetConverter(dic, typeof(T), nothrow);
        }

        internal static IPacketConverter GetConverter(ConverterDictionary dic, Type typ, bool nothrow)
        {
            if (typ == null)
                throw new ArgumentNullException(nameof(typ));
            if (dic != null && dic.TryGetValue(typ, out var val))
                if (val == null)
                    goto fail;
                else return val;
            if (s_converters.TryGetValue(typ, out val))
                return val;

            var inf = GetInfo(typ);
            if (inf.Flag == _Inf.Enum)
                return s_converters[inf.ElementType];

            fail:
            if (nothrow == true)
                return null;
            throw PacketException.InvalidType(typ);
        }

        internal static IPacketConverter GetConverterInternal(ConverterDictionary dic, Type typ)
        {
            if (dic != null && dic.TryGetValue(typ, out var val))
                return val;
            if (s_converters.TryGetValue(typ, out val))
                return val;
            return null;
        }

        internal static bool TryGetConverter(ConverterDictionary dic, Type typ, out IPacketConverter val)
        {
            if (dic != null && dic.TryGetValue(typ, out val))
                return true;
            if (s_converters.TryGetValue(typ, out val))
                return true;
            return false;
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value)
        {
            var con = GetConverter(dic, type, false);
            var buf = con.GetBytesWrap(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary dic, T value)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> res)
                return res.GetBytesWrap(value);
            return con.GetBytesWrap(value);
        }

        internal static byte[][] GetBytesFromEnumerableNonGeneric(IPacketConverter con, IEnumerable itr)
        {
            var lst = new List<byte[]>();
            foreach (var i in itr)
                lst.Add(con.GetBytesWrap(i));
            return lst.ToArray();
        }

        internal static byte[][] GetBytesFromArray<T>(IPacketConverter con, T[] arr)
        {
            var res = new byte[arr.Length][];
            if (con is IPacketConverter<T> gen)
                for (int i = 0; i < arr.Length; i++)
                    res[i] = gen.GetBytesWrap(arr[i]);
            else
                for (int i = 0; i < arr.Length; i++)
                    res[i] = con.GetBytesWrap(arr[i]);
            return res;
        }

        internal static byte[][] GetBytesFromList<T>(IPacketConverter con, List<T> arr)
        {
            var res = new byte[arr.Count][];
            if (con is IPacketConverter<T> gen)
                for (int i = 0; i < arr.Count; i++)
                    res[i] = gen.GetBytesWrap(arr[i]);
            else
                for (int i = 0; i < arr.Count; i++)
                    res[i] = con.GetBytesWrap(arr[i]);
            return res;
        }

        internal static byte[][] GetBytesFromEnumerable<T>(IPacketConverter con, IEnumerable<T> itr)
        {
            if (itr is ICollection<T> col && col.Count > 15)
                return GetBytesFromArray(con, col.ToArray());

            var res = new List<byte[]>();
            if (con is IPacketConverter<T> gen)
                foreach (var i in itr)
                    res.Add(gen.GetBytesWrap(i));
            else
                foreach (var i in itr)
                    res.Add(con.GetBytesWrap(i));
            return res.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> GetBytesFromDictionary<TK, TV>(IPacketConverter keycon, IPacketConverter valcon, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var res = new List<KeyValuePair<byte[], byte[]>>();
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var val = i.Value;
                var keybuf = (keygen != null ? keygen.GetBytesWrap(key) : keycon.GetBytesWrap(key));
                var valbuf = (valgen != null ? valgen.GetBytesWrap(val) : valcon.GetBytesWrap(val));
                res.Add(new KeyValuePair<byte[], byte[]>(keybuf, valbuf));
            }
            return res;
        }
    }
}
