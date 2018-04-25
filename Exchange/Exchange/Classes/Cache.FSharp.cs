using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        private const string FSharpCollectionsNamespace = "Microsoft.FSharp.Collections";

        private static MethodInfo s_to_fslist;
        private static MethodInfo s_to_tuple_list = typeof(Convert).GetMethod(nameof(Convert.ToTupleList), Flags);
        private static MethodInfo s_cast_tuple_list = typeof(Convert).GetMethod(nameof(Convert.ToTupleListCast), Flags);

        private static bool IsFSharpList(Type type)
        {
            if (type.Name != "FSharpList`1" || type.Namespace != FSharpCollectionsNamespace)
                return false;
            var fun = s_to_fslist;
            if (fun != null)
                return true;
            var mod = type.Assembly.GetType("Microsoft.FSharp.Collections.ArrayModule", false, false);
            if (mod == null)
                return false;
            var met = mod.GetMethods().Where(r => r.Name == "ToList").ToArray();
            if (met.Length != 1)
                return false;
            s_to_fslist = met[0];
            return true;
        }

        private static bool IsFSharpMap(Type type, Type[] elementTypes, out ConstructorInfo constructorInfo)
        {
            if (type.Name != "FSharpMap`2" || type.Namespace != FSharpCollectionsNamespace)
                goto fail;
            var con = type.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(typeof(Tuple<,>).MakeGenericType(elementTypes)) });
            if (con == null)
                goto fail;
            constructorInfo = con;
            return true;

        fail:
            constructorInfo = null;
            return false;
        }

        private static Func<PacketReader, IPacketConverter, object> GetToFSharpListFunction(Type elementType)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = s_to_array.MakeGenericMethod(elementType);
            var cal = Expression.Call(met, rea, con);
            var cvt = s_to_fslist.MakeGenericMethod(elementType);
            var inv = Expression.Call(cvt, cal);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(box, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastFSharpListFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var cvt = s_to_fslist.MakeGenericMethod(elementType);
            var inv = Expression.Call(cvt, blk);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, IPacketConverter, object> GetToFSharpMapFunction(ConstructorInfo constructorInfo, params Type[] types)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var key = Expression.Parameter(typeof(IPacketConverter), "key");
            var val = Expression.Parameter(typeof(IPacketConverter), "value");
            var met = s_to_tuple_list.MakeGenericMethod(types);
            var cal = Expression.Call(met, rea, key, val);
            var inv = Expression.New(constructorInfo, cal);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, IPacketConverter, object>>(box, rea, key, val);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<List<object>, object> GetCastFSharpMapFunction(ConstructorInfo constructorInfo, params Type[] types)
        {
            var arr = Expression.Parameter(typeof(List<object>), "list");
            var met = s_cast_tuple_list.MakeGenericMethod(types);
            var cal = Expression.Call(met, arr);
            var inv = Expression.New(constructorInfo, cal);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<List<object>, object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }
    }
}
