using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Network
{
    partial class Cache
    {
        private static void CreateInfoFromDictionary(Info inf, Info itr, params Type[] types)
        {
            if (types[0] == typeof(string) && types[1] == typeof(object))
                inf.From = Info.Map;
            else
                inf.From = Info.Dictionary;
            inf.IndexType = types[0];
            inf.ElementType = types[1];
            inf.FromDictionary = itr.FromDictionary;
            inf.FromDictionaryAdapter = itr.FromDictionaryAdapter;
        }

        private static Info CreateInfo(Type typ)
        {
            var inf = new Info() { Type = typ };
            if (typ == typeof(PacketWriter))
            {
                inf.From = Info.Writer;
                return inf;
            }
            else if (typ == typeof(PacketRawWriter))
            {
                inf.From = Info.RawWriter;
                return inf;
            }
            else if (typ == typeof(PacketReader) || typ == typeof(object))
            {
                inf.To = Info.Reader;
                return inf;
            }
            else if (typ == typeof(PacketRawReader))
            {
                inf.To = Info.RawReader;
                return inf;
            }
            if (typ.IsEnum)
            {
                inf.Flag = Info.Enum;
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
                    inf.To = Info.Enumerable;
                    inf.ToEnumerable = GetToFunction(s_to_enumerable, ele);
                    inf.ToEnumerableAdapter = GetToEnumerableAdapter(ele);

                    // From ... function
                    inf.From = Info.Enumerable;
                    inf.FromEnumerable = GetFromEnumerableFunction(s_from_enumerable.MakeGenericMethod(ele), typ);
                    if (ele.IsGenericType && ele.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    {
                        var arg = ele.GetGenericArguments();
                        inf.FromDictionary = GetFromDictionaryFunction(arg);
                        inf.FromDictionaryAdapter = GetFromAdapterFunction(arg);
                    }
                    return inf;
                }
                if (genericDefinition == typeof(ICollection<>))
                {
                    var ele = genericArgs[0];
                    inf.ElementType = ele;
                    inf.To = Info.Collection; // to icollection
                    var arr = GetInfo(ele.MakeArrayType());
                    inf.ToCollection = arr.ToCollection;
                    inf.ToCollectionCast = arr.ToCollectionCast;
                    return inf;
                }
                if (genericDefinition == typeof(IList<>))
                {
                    var ele = genericArgs[0];
                    inf.ElementType = ele;
                    inf.To = Info.Collection; // to list
                    inf.ToCollection = GetToFunction(s_to_list, ele);
                    inf.ToCollectionCast = GetCastFunction(s_cast_list, ele);
                    return inf;
                }
                if (genericDefinition == typeof(IDictionary<,>))
                {
                    var kvp = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
                    var itr = typeof(IEnumerable<>).MakeGenericType(kvp);
                    var sub = GetInfo(itr);
                    CreateInfoFromDictionary(inf, sub, genericArgs);
                    inf.To = Info.Dictionary;
                    inf.ToDictionary = GetToDictionaryFunction(genericArgs);
                    inf.ToDictionaryCast = GetCastDictionaryFunction(genericArgs);
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

                inf.From = Info.Enumerable;
                inf.To = Info.Collection; // to array
                inf.FromEnumerable = GetFromEnumerableFunction(s_from_array.MakeGenericMethod(ele), typ);
                inf.ToCollection = GetToFunction(s_to_array, ele);
                inf.ToCollectionCast = GetCastFunction(s_cast_array, ele);
            }
            else if (generic && genericArgs.Length == 1)
            {
                var ele = genericArgs[0];
                if (genericDefinition == typeof(List<>))
                {
                    var sub = GetInfo(typeof(IList<>).MakeGenericType(ele));
                    inf.ElementType = ele;
                    inf.From = Info.Enumerable;
                    inf.To = Info.Collection; // to list
                    inf.FromEnumerable = GetFromEnumerableFunction(s_from_list.MakeGenericMethod(ele), typ);
                    inf.ToCollection = sub.ToCollection;
                    inf.ToCollectionCast = sub.ToCollectionCast;
                }
                else
                {
                    var fun = GetToCollectionFunction(typ, ele, out var ctor);
                    if (fun != null)
                    {
                        inf.ElementType = ele;
                        inf.To = Info.Collection;
                        inf.ToCollection = fun;
                        inf.ToCollectionCast = GetCastCollectionFunction(ele, ctor);
                    }
                }
            }
            else if (generic && genericArgs.Length == 2)
            {
                if (genericDefinition == typeof(Dictionary<,>))
                {
                    inf.To = Info.Dictionary;
                    inf.ToDictionary = GetToDictionaryFunction(genericArgs[0], genericArgs[1]);
                    inf.ToDictionaryCast = GetCastDictionaryFunction(genericArgs[0], genericArgs[1]);
                }
            }

            var interfaces = typ.GetInterfaces();
            if (inf.From == Info.None)
            {
                if (interfaces.Contains(typeof(IDictionary<string, object>)))
                    inf.From = Info.Map;

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
                        CreateInfoFromDictionary(inf, lst[0], ele.GetGenericArguments());
                    }
                    else
                    {
                        inf.From = Info.Enumerable;
                        inf.ElementType = itr.ElementType;
                        inf.FromEnumerable = itr.FromEnumerable;
                    }
                }
            }

            if (inf.From == Info.Enumerable)
            {
                if (inf.ElementType == typeof(byte) && interfaces.Contains(typeof(ICollection<byte>)))
                    inf.From = Info.Bytes;
                else if (inf.ElementType == typeof(sbyte) && interfaces.Contains(typeof(ICollection<sbyte>)))
                    inf.From = Info.SBytes;
            }
            return inf;
        }

        internal static Info GetInfo(Type type)
        {
            if (s_infos.TryGetValue(type, out var inf))
                return inf;
            return s_infos.GetOrAdd(type, CreateInfo(type));
        }
    }
}
