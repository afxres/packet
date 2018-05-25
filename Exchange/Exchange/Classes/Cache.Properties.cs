using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        private static GetInfo CreateGetInfo(Type type)
        {
            var inf = new List<KeyValuePair<string, Type>>();
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
                inf.Add(new KeyValuePair<string, Type>(cur.Name, cur.PropertyType));
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

            var res = new GetInfo(inf.ToArray(), del.Compile());
            return res;
        }

        private static SetInfo CreateSetInfoAnonymousType(Type type)
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
            var inf = new KeyValuePair<string, Type>[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var cur = arg[i];
                var idx = Expression.ArrayIndex(ipt, Expression.Constant(i));
                var cvt = Expression.Convert(idx, cur.ParameterType);
                arr[i] = cvt;
                inf[i] = new KeyValuePair<string, Type>(cur.Name, cur.ParameterType);
            }

            // Reference type
            var ins = Expression.New(con, arr);
            var del = Expression.Lambda<Func<object[], object>>(ins, ipt);
            var res = new SetInfo(inf, del.Compile());
            return res;
        }

        private static SetInfo CreateSetInfo(Type type, ConstructorInfo constructor)
        {
            var pro = type.GetProperties();
            var ins = (constructor == null) ? Expression.New(type) : Expression.New(constructor);
            var inf = new List<KeyValuePair<string, Type>>();
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
                inf.Add(new KeyValuePair<string, Type>(cur.Name, cur.PropertyType));
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
                var cvt = Expression.Convert(idx, inf[i].Value);
                var set = Expression.Call(val, met[i], cvt);
                exp.Add(set);
            }

            var cst = Expression.Convert(val, typeof(object));
            exp.Add(cst);

            var blk = Expression.Block(new[] { val }, exp);
            var del = Expression.Lambda<Func<object[], object>>(blk, ipt);
            var res = new SetInfo(inf.ToArray(), del.Compile());
            return res;
        }

        private static SetInfo CreateSetInfo(Type type)
        {
            if (type.IsValueType)
                return CreateSetInfo(type, null);
            var con = type.GetConstructor(Type.EmptyTypes);
            if (con != null)
                return CreateSetInfo(type, con);
            return CreateSetInfoAnonymousType(type);
        }

        internal static GetInfo GetGetInfo(Type type)
        {
            if (GetInfos.TryGetValue(type, out var inf))
                return inf;
            return GetInfos.GetOrAdd(type, CreateGetInfo(type));
        }

        internal static SetInfo GetSetInfo(Type type)
        {
            if (SetInfos.TryGetValue(type, out var inf))
                return inf;
            return SetInfos.GetOrAdd(type, CreateSetInfo(type));
        }
    }
}
