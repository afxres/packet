using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class _Caches
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo s_from_enumerable = typeof(_Caches).GetMethod(nameof(_FromEnumerable), Flags);
        private static readonly MethodInfo s_from_enumerable_key_value_pair = typeof(_Caches).GetMethod(nameof(_FromDictionary), Flags);

        private static readonly MethodInfo s_cast_array = typeof(_Convert).GetMethod(nameof(_Convert.ToArrayCast), Flags);
        private static readonly MethodInfo s_cast_list = typeof(_Convert).GetMethod(nameof(_Convert.ToListCast), Flags);
        private static readonly MethodInfo s_cast_dictionary = typeof(_Convert).GetMethod(nameof(_Convert.ToDictionaryCast), Flags);

        private static readonly MethodInfo s_to_array = typeof(_Convert).GetMethod(nameof(_Convert.ToArray), Flags);
        private static readonly MethodInfo s_to_list = typeof(_Convert).GetMethod(nameof(_Convert.ToList), Flags);
        private static readonly MethodInfo s_to_collection = typeof(_Convert).GetMethod(nameof(_Convert.ToCollection), Flags);
        private static readonly MethodInfo s_to_enumerable = typeof(_Convert).GetMethod(nameof(_Convert.ToEnumerable), Flags);
        private static readonly MethodInfo s_to_dictionary = typeof(_Convert).GetMethod(nameof(_Convert.ToDictionary), Flags);

        private static Func<PacketReader, IPacketConverter, object> _GetToFunction(MethodInfo info, Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(cal, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, object> _GetToCollectionFunction(Type type, Type element, out ConstructorInfo info)
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

        private static Func<PacketReader, IPacketConverter, IPacketConverter, object> _GetToDictionaryFunction(params Type[] types)
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

        private static Func<PacketReader, int, object> _GetToEnumerableAdapter(Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var lev = Expression.Parameter(typeof(int), "level");
            var cto = typeof(_EnumerableReader<>).MakeGenericType(element).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            var inv = Expression.New(cto, rea, lev);
            var exp = Expression.Lambda<Func<PacketReader, int, object>>(inv, rea, lev);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> _GetCastFunction(MethodInfo info, Type element)
        {
            var arr = Expression.Parameter(typeof(object[]), "array");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<object[], object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> _GetCastCollectionFunction(Type element, ConstructorInfo info)
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

        private static Func<IEnumerable<KeyValuePair<object, object>>, object> _GetCastDictionaryFunction(params Type[] types)
        {
            var arr = Expression.Parameter(typeof(IEnumerable<KeyValuePair<object, object>>), "pairs");
            var met = s_cast_dictionary.MakeGenericMethod(types);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<IEnumerable<KeyValuePair<object, object>>, object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, MemoryStream> _GetFromEnumerableFunction(Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var obj = Expression.Parameter(typeof(object), "object");
            var met = s_from_enumerable.MakeGenericMethod(element);
            var cal = Expression.Call(met, con, obj);
            var exp = Expression.Lambda<Func<IPacketConverter, object, MemoryStream>>(cal, con, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, IPacketConverter, object, MemoryStream> _GetFromDictionaryFunction(params Type[] types)
        {
            var key = Expression.Parameter(typeof(IPacketConverter), "index");
            var val = Expression.Parameter(typeof(IPacketConverter), "element");
            var obj = Expression.Parameter(typeof(object), "object");
            var met = s_from_enumerable_key_value_pair.MakeGenericMethod(types);
            var cal = Expression.Call(met, key, val, obj);
            var exp = Expression.Lambda<Func<IPacketConverter, IPacketConverter, object, MemoryStream>>(cal, key, val, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> _GetFromAdapterFunction(params Type[] types)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(types));
            var cto = typeof(_EnumerableAdapter<,>).MakeGenericType(types).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
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
