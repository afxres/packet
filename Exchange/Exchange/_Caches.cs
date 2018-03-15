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

        private static readonly ConcurrentDictionary<Type, _Inf> s_info = new ConcurrentDictionary<Type, _Inf>();
        private static readonly ConcurrentDictionary<Type, GetterInfo> s_getter = new ConcurrentDictionary<Type, GetterInfo>();
        private static readonly ConcurrentDictionary<Type, SetterInfo> s_setter = new ConcurrentDictionary<Type, SetterInfo>();

        internal static void ClearCache()
        {
            s_info.Clear();
            s_getter.Clear();
            s_setter.Clear();
        }

        private static void _CreateInfoSetKeyValuePair(_Inf inf, _Inf enumerable, Type index, Type element)
        {
            inf.Flags |= _Inf.EnumerableKeyValuePair;
            inf.IndexType = index;
            inf.ElementType = element;
            inf.FromEnumerableKeyValuePair = enumerable.FromEnumerableKeyValuePair;
            inf.GetEnumerableKeyValuePairAdapter = enumerable.GetEnumerableKeyValuePairAdapter;
        }

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
                    var fun = default(Func<PacketReader, IPacketConverter, object>);

                    if (def == typeof(IEnumerable<>))
                    {
                        inf.Flags |= _Inf.Enumerable;
                        inf.GetEnumerable = _CreateGetFunction(s_get_enumerable, one);
                        inf.GetEnumerableReader = _CreateGetEnumerableReaderFunction(one);

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
                        _CreateInfoSetKeyValuePair(inf, itr, arg[0], arg[1]);

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
                    _CreateInfoSetKeyValuePair(inf, lst[0], kvp.IndexType, kvp.ElementType);
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

        private static MemoryStream _GetStreamFromEnumerable(IPacketConverter con, IEnumerable itr)
        {
            var mst = new MemoryStream(_Length);
            var def = con.Length;
            if (def > 0)
            {
                foreach (var i in itr)
                {
                    mst.Write(con.GetBytesWrap(i), 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con.GetBytesWrap(i);
                    var len = buf.Length;
                    var pre = (len == 0 ? s_zero_bytes : BitConverter.GetBytes(len));
                    mst.Write(pre, 0, sizeof(int));
                    mst.Write(buf, 0, len);
                }
            }
            return mst;
        }

        private static MemoryStream _GetStreamFromEnumerableGeneric<T>(IPacketConverter<T> con, IEnumerable<T> itr)
        {
            var mst = new MemoryStream(_Length);
            var def = con.Length;
            if (def > 0)
            {
                foreach (var i in itr)
                {
                    mst.Write(con.GetBytesWrap(i), 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con.GetBytesWrap(i);
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
                var keybuf = (keygen != null ? keygen.GetBytesWrap(key) : keycon.GetBytesWrap(key));
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
                var valbuf = (valgen != null ? valgen.GetBytesWrap(val) : valcon.GetBytesWrap(val));
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

        private static MemoryStream _FromEnumerable<T>(IPacketConverter con, object obj)
        {
            if (con is IPacketConverter<T> gen)
                return _GetStreamFromEnumerableGeneric(gen, (IEnumerable<T>)obj);
            else return _GetStreamFromEnumerable(con, (IEnumerable)obj);
        }

        private static MemoryStream _FromEnumerableKeyValuePair<TK, TV>(IPacketConverter key, IPacketConverter val, object obj)
        {
            return _GetStreamGeneric(key, val, (IEnumerable<KeyValuePair<TK, TV>>)obj);
        }

        internal static MemoryStream GetStream(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = GetConverter(dic, type, false);
            var mst = _GetStreamFromEnumerable(con, itr);
            return mst;
        }

        internal static MemoryStream GetStreamGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> gen)
                return _GetStreamFromEnumerableGeneric(gen, itr);
            return _GetStreamFromEnumerable(con, itr);
        }

        internal static MemoryStream GetStreamGeneric<TK, TV>(ConverterDictionary dic, IEnumerable<KeyValuePair<TK, TV>> itr)
        {
            var key = GetConverter<TK>(dic, false);
            var val = GetConverter<TV>(dic, false);
            var mst = _GetStreamGeneric(key, val, itr);
            return mst;
        }
    }
}
