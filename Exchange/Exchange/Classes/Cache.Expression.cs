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

        private static readonly MethodInfo s_cast_array = typeof(Convert).GetMethod(nameof(Convert.ToArrayCast), Flags);
        private static readonly MethodInfo s_cast_list = typeof(Convert).GetMethod(nameof(Convert.ToListCast), Flags);
        private static readonly MethodInfo s_cast_dictionary = typeof(Convert).GetMethod(nameof(Convert.ToDictionaryCast), Flags);

        private static readonly MethodInfo s_to_array = typeof(Convert).GetMethod(nameof(Convert.ToArray), Flags);
        private static readonly MethodInfo s_to_list = typeof(Convert).GetMethod(nameof(Convert.ToList), Flags);
        private static readonly MethodInfo s_to_collection = typeof(Convert).GetMethod(nameof(Convert.ToCollection), Flags);
        private static readonly MethodInfo s_to_enumerable = typeof(Convert).GetMethod(nameof(Convert.ToEnumerable), Flags);
        private static readonly MethodInfo s_to_dictionary = typeof(Convert).GetMethod(nameof(Convert.ToDictionary), Flags);

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

        private static Func<PacketReader, int, Info, object> _GetToEnumerableAdapter(Type element)
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

        private static Func<List<KeyValuePair<object, object>>, object> _GetCastDictionaryFunction(params Type[] types)
        {
            var arr = Expression.Parameter(typeof(List<KeyValuePair<object, object>>), "pairs");
            var met = s_cast_dictionary.MakeGenericMethod(types);
            var cal = Expression.Call(met, arr);
            var exp = Expression.Lambda<Func<List<KeyValuePair<object, object>>, object>>(cal, arr);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, object, byte[][]> _GetFromEnumerableFunction(MethodInfo method, Type enumerable)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var obj = Expression.Parameter(typeof(object), "object");
            var cvt = Expression.Convert(obj, enumerable);
            var cal = Expression.Call(method, con, cvt);
            var exp = Expression.Lambda<Func<IPacketConverter, object, byte[][]>>(cal, con, obj);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<IPacketConverter, IPacketConverter, object, List<KeyValuePair<byte[], byte[]>>> _GetFromDictionaryFunction(params Type[] types)
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

        private static Func<IPacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> _GetFromAdapterFunction(params Type[] types)
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
