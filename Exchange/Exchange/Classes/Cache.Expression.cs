using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo s_from_array = typeof(Cache).GetMethod(nameof(GetBytesFromArray), Flags);
        private static readonly MethodInfo s_from_list = typeof(Cache).GetMethod(nameof(GetBytesFromList), Flags);
        private static readonly MethodInfo s_from_enumerable = typeof(Cache).GetMethod(nameof(GetBytesFromEnumerable), Flags);
        private static readonly MethodInfo s_from_dictionary = typeof(Cache).GetMethod(nameof(GetBytesFromDictionary), Flags);

        private static readonly MethodInfo s_cast_dictionary = typeof(Convert).GetMethod(nameof(Convert.ToDictionaryCast), Flags);

        private static readonly MethodInfo s_to_array = typeof(Convert).GetMethod(nameof(Convert.ToArray), Flags);
        private static readonly MethodInfo s_to_list = typeof(Convert).GetMethod(nameof(Convert.ToList), Flags);
        private static readonly MethodInfo s_to_collection = typeof(Convert).GetMethod(nameof(Convert.ToCollection), Flags);
        private static readonly MethodInfo s_to_enumerable = typeof(Convert).GetMethod(nameof(Convert.ToEnumerable), Flags);
        private static readonly MethodInfo s_to_dictionary = typeof(Convert).GetMethod(nameof(Convert.ToDictionary), Flags);

        private static readonly MethodInfo s_array_copy = typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(Array), typeof(int) });

        private static Func<PacketReader, IPacketConverter, object> GetToFunction(MethodInfo info, Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(cal, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, object> GetToCollectionFunction(Type type, Type element, out ConstructorInfo info)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var cto = type.GetConstructor(new[] { itr });
            info = cto;
            if (cto == null)
                return null;
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = s_to_collection.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var cst = Expression.Convert(cal, itr);
            var inv = Expression.New(cto, cst);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(box, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, IPacketConverter, object> GetToDictionaryFunction(params Type[] types)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var key = Expression.Parameter(typeof(IPacketConverter), "key");
            var val = Expression.Parameter(typeof(IPacketConverter), "value");
            var met = s_to_dictionary.MakeGenericMethod(types);
            var cal = Expression.Call(met, rea, key, val);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, IPacketConverter, object>>(cal, rea, key, val);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, int, Info, object> GetToEnumerableAdapter(Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var lev = Expression.Parameter(typeof(int), "level");
            var inf = Expression.Parameter(typeof(Info), "info");
            var cto = typeof(EnumerableAdapter<>).MakeGenericType(element).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var inv = Expression.New(cto, rea, lev, inf);
            var exp = Expression.Lambda<Func<PacketReader, int, Info, object>>(inv, rea, lev, inf);
            var fun = exp.Compile();
            return fun;
        }

        private static Expression GetCastArrayExpression(Type elementType, out ParameterExpression parameter)
        {
            parameter = Expression.Parameter(typeof(object[]), "parameter");
            if (elementType == typeof(object))
                return parameter;
            var len = Expression.ArrayLength(parameter);
            var dst = Expression.NewArrayBounds(elementType, len);
            var loc = Expression.Variable(elementType.MakeArrayType(), "destination");
            var ass = Expression.Assign(loc, dst);
            var cpy = Expression.Call(s_array_copy, parameter, loc, len);
            var blk = Expression.Block(new ParameterExpression[] { loc }, new Expression[] { ass, cpy, loc });
            return blk;
        }

        private static Func<object[], object> GetCastArrayFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var exp = Expression.Lambda<Func<object[], object>>(blk, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastListFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var con = typeof(List<>).MakeGenericType(elementType).GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
            var inv = Expression.New(con, blk);
            var exp = Expression.Lambda<Func<object[], object>>(inv, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastCollectionFunction(Type elementType, ConstructorInfo constructorInfo)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var inv = Expression.New(constructorInfo, blk);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<List<KeyValuePair<object, object>>, object> GetCastDictionaryFunction(params Type[] types)
        {
            var arr = Expression.Parameter(typeof(List<KeyValuePair<object, object>>), "dictionary");
            var met = s_cast_dictionary.MakeGenericMethod(types);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<List<KeyValuePair<object, object>>, object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, byte[][]> GetFromEnumerableFunction(MethodInfo method, Type enumerable)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, enumerable);
            var cal = Expression.Call(method, con, cvt);
            var exp = Expression.Lambda<Func<IPacketConverter, object, byte[][]>>(cal, con, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, IPacketConverter, object, List<KeyValuePair<byte[], byte[]>>> GetFromDictionaryFunction(params Type[] types)
        {
            var key = Expression.Parameter(typeof(IPacketConverter), "index");
            var val = Expression.Parameter(typeof(IPacketConverter), "element");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(types)));
            var met = s_from_dictionary.MakeGenericMethod(types);
            var cal = Expression.Call(met, key, val, cvt);
            var exp = Expression.Lambda<Func<IPacketConverter, IPacketConverter, object, List<KeyValuePair<byte[], byte[]>>>>(cal, key, val, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> GetFromAdapterFunction(params Type[] types)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(types));
            var cto = typeof(DictionaryAdapter<,>).MakeGenericType(types).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var key = Expression.Parameter(typeof(IPacketConverter), "index");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, itr);
            var inv = Expression.New(cto, key, cvt);
            var exp = Expression.Lambda<Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>>>(inv, key, obj);
            var fun = exp.Compile();
            return fun;
        }
    }
}
