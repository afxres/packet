using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Mikodev.Network._Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class _Caches
    {
        internal const int _Length = 256;
        internal const int _Depth = 64;

        private static readonly MethodInfo s_get_lst = typeof(_Element).GetMethod(nameof(_Element.List), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_arr = typeof(_Element).GetMethod(nameof(_Element.Array), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_get_seq = typeof(_Caches).GetMethod(nameof(_GetSequenceAuto), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly ConditionalWeakTable<Type, _DetailInfo> s_detail = new ConditionalWeakTable<Type, _DetailInfo>();
        private static readonly ConditionalWeakTable<Type, Func<PacketReader, object>> s_itr = new ConditionalWeakTable<Type, Func<PacketReader, object>>();

        private static readonly ConditionalWeakTable<Type, SolveInfo> s_slv = new ConditionalWeakTable<Type, SolveInfo>();
        private static readonly ConditionalWeakTable<Type, DissoInfo> s_dis = new ConditionalWeakTable<Type, DissoInfo>();

        private static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_arr = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();
        private static readonly ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>> s_lst = new ConditionalWeakTable<Type, Func<_Element, IPacketConverter, object>>();

        private static readonly ConditionalWeakTable<Type, Func<IPacketConverter, object, MemoryStream>> s_seq = new ConditionalWeakTable<Type, Func<IPacketConverter, object, MemoryStream>>();

        internal static _DetailInfo GetDetail(Type type)
        {
            if (s_detail.TryGetValue(type, out var res))
                return res;
            var inf = new _DetailInfo();

            var tag = type.IsEnum;
            inf.is_enum = tag;
            inf.arg_enum = tag ? Enum.GetUnderlyingType(type) : null;
            if (tag == true)
                return s_detail.GetValue(type, _Wrap(inf).Value);

            var arr = type.IsArray && type.GetArrayRank() == 1;
            inf.is_arr = arr;
            inf.arg_of_arr = arr ? type.GetElementType() : null;

            var gen = type.IsGenericType;
            var def = gen ? type.GetGenericTypeDefinition() : null;
            var arg = gen ? type.GetGenericArguments() : null;
            var one = gen ? arg.Length == 1 : false;
            var lst = one && (def == typeof(List<>) || def == typeof(IList<>));
            var itr = one && (def == typeof(IEnumerable<>));
            inf.is_lst = lst;
            inf.is_itr = itr;
            inf.arg_of_lst = lst ? arg[0] : null;
            inf.arg_of_itr = itr ? arg[0] : null;

            var imp = default(Type);
            foreach (var i in type.GetInterfaces())
            {
                var det = GetDetail(i);
                if (det.is_itr == false)
                    continue;
                imp = det.arg_of_itr;
            }
            inf.is_itr_imp = (imp != null);
            inf.arg_of_itr_imp = imp;

            return s_detail.GetValue(type, _Wrap(inf).Value);
        }

        internal static object GetList(PacketReader reader, Type type)
        {
            var con = GetConverter(reader._cvt, type, false);
            if (s_lst.TryGetValue(type, out var val) == false)
            {
                var met = s_get_lst.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(ele, met, arg);
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
                var com = fun.Compile();
                val = s_lst.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader._spa, con);
        }

        internal static object GetArray(PacketReader reader, Type type)
        {
            var con = GetConverter(reader._cvt, type, false);
            if (s_arr.TryGetValue(type, out var val) == false)
            {
                var met = s_get_arr.MakeGenericMethod(type);
                var ele = Expression.Parameter(typeof(_Element), "element");
                var arg = Expression.Parameter(typeof(IPacketConverter), "converter");
                var inv = Expression.Call(ele, met, arg);
                var fun = Expression.Lambda<Func<_Element, IPacketConverter, object>>(inv, ele, arg);
                var com = fun.Compile();
                val = s_arr.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader._spa, con);
        }

        internal static object GetEnumerable(PacketReader reader, Type type)
        {
            if (s_itr.TryGetValue(type, out var val) == false)
            {
                var typ = typeof(_Enumerable<>).MakeGenericType(type);
                var cts = typ.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

                var par = Expression.Parameter(typeof(PacketReader), "reader");
                var inv = Expression.New(cts[0], par);
                var fun = Expression.Lambda<Func<PacketReader, object>>(inv, par);
                var com = fun.Compile();
                val = s_itr.GetValue(type, _Wrap(com).Value);
            }
            return val.Invoke(reader);
        }

        internal static SolveInfo GetGetMethods(Type type)
        {
            if (s_slv.TryGetValue(type, out var sol))
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
            return s_slv.GetValue(type, _Wrap(res).Value);
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

            return s_dis.GetValue(type, _Wrap(res).Value);
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
            return s_dis.GetValue(type, _Wrap(res).Value);
        }

        internal static DissoInfo GetSetMethods(Type type)
        {
            if (s_dis.TryGetValue(type, out var val))
                return val;

            if (type.IsValueType)
                return _DissolveType(type, null);
            var con = type.GetConstructor(Type.EmptyTypes);
            if (con != null)
                return _DissolveType(type, con);
            return _DissolveAnonymousType(type);
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
            if (s_dic.TryGetValue(type, out val))
                return val;

            var det = GetDetail(type);
            if (det.is_enum && s_dic.TryGetValue(det.arg_enum, out val))
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
                    var buf = con._GetBytesWrapError(i);
                    mst.Write(buf, 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapError(i);
                    var len = buf.Length;
                    var pre = (len == 0)
                        ? s_zero_bytes
                        : BitConverter.GetBytes(len);
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
                    var buf = con._GetBytesWrapErrorGeneric(i);
                    mst.Write(buf, 0, def);
                }
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapErrorGeneric(i);
                    var len = buf.Length;
                    var pre = (len == 0)
                        ? s_zero_bytes
                        : BitConverter.GetBytes(len);
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
            var con = _Caches.GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> gen)
                return _GetSequenceGeneric(gen, itr);
            return _GetSequence(con, itr);
        }

        internal static MemoryStream GetSequence(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = _Caches.GetConverter(dic, type, false);
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
            if (s_seq.TryGetValue(type, out var val) == false)
            {
                var inf = s_get_seq.MakeGenericMethod(type);
                var cvt = Expression.Parameter(typeof(IPacketConverter), "converter");
                var enu = Expression.Parameter(typeof(object), "enumerable");
                var cst = Expression.TypeAs(enu, typeof(IEnumerable<>).MakeGenericType(type));
                var cal = Expression.Call(inf, cvt, cst);
                var fun = Expression.Lambda<Func<IPacketConverter, object, MemoryStream>>(cal, cvt, enu);
                var com = fun.Compile();
                val = s_seq.GetValue(type, _Wrap(com).Value);
            }
            var seq = val.Invoke(con, itr);
            return seq;
        }
    }
}
