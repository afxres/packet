using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class _Caches
    {
        private const BindingFlags _Flags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly MethodInfo s_from_enumerable = typeof(_Caches).GetMethod(nameof(_FromEnumerable), _Flags);
        private static readonly MethodInfo s_from_enumerable_key_value_pair = typeof(_Caches).GetMethod(nameof(_FromEnumerableKeyValuePair), _Flags);

        private static readonly MethodInfo s_cast_array = typeof(_Convert).GetMethod(nameof(_Convert.CastToArray), _Flags);
        private static readonly MethodInfo s_cast_list = typeof(_Convert).GetMethod(nameof(_Convert.CastToList), _Flags);
        private static readonly MethodInfo s_cast_dictionary = typeof(_Convert).GetMethod(nameof(_Convert.CastToDictionary), _Flags);

        private static readonly MethodInfo s_get_array = typeof(_Convert).GetMethod(nameof(_Convert.GetArray), _Flags);
        private static readonly MethodInfo s_get_list = typeof(_Convert).GetMethod(nameof(_Convert.GetList), _Flags);
        private static readonly MethodInfo s_get_collection = typeof(_Convert).GetMethod(nameof(_Convert.GetCollection), _Flags);
        private static readonly MethodInfo s_get_enumerable = typeof(_Convert).GetMethod(nameof(_Convert.GetEnumerable), _Flags);
        private static readonly MethodInfo s_get_dictionary = typeof(_Convert).GetMethod(nameof(_Convert.GetDictionary), _Flags);

        private static Func<PacketReader, IPacketConverter, object> _CreateGetFunction(MethodInfo info, Type element)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = info.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(cal, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, object> _CreateGetCollectionFunction(Type type, Type element, out ConstructorInfo info)
        {
            var itr = typeof(IEnumerable<>).MakeGenericType(element);
            var cto = type.GetConstructor(new[] { itr });
            info = cto;
            if (cto == null)
                return null;
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = s_get_collection.MakeGenericMethod(element);
            var cal = Expression.Call(met, rea, con);
            var cst = Expression.Convert(cal, itr);
            var inv = Expression.New(cto, cst);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(box, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, IPacketConverter, IPacketConverter, object> _CreateGetDictionaryFunction(Type index, Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var key = Expression.Parameter(typeof(IPacketConverter), "key");
            var val = Expression.Parameter(typeof(IPacketConverter), "value");
            var met = s_get_dictionary.MakeGenericMethod(index, element);
            var cal = Expression.Call(met, rea, key, val);
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, IPacketConverter, object>>(cal, rea, key, val);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<PacketReader, int, object> _CreateGetEnumerableReaderFunction(Type element)
        {
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var lev = Expression.Parameter(typeof(int), "level");
            var cto = typeof(_EnumerableReader<>).MakeGenericType(element).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
            var inv = Expression.New(cto, rea, lev);
            var exp = Expression.Lambda<Func<PacketReader, int, object>>(inv, rea, lev);
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
    }
}
