using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Network.Internal
{
    internal static partial class Convert
    {
        #region to array, to list

        private static List<T> InternalToList<T>(PacketReader reader, PacketConverter converter)
        {
            var readerList = reader.GetList();
            var count = readerList.Count;
            if (count < 1)
                return new List<T>();

            try
            {
                var result = new List<T>(count);
                var generic = (PacketConverter<T>)converter;
                for (var i = 0; i < count; i++)
                    result.Add(generic.GetValue(readerList[i].block));
                return result;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        private static T[] InternalToArray<T>(PacketReader reader, PacketConverter converter)
        {
            var readerList = reader.GetList();
            var count = readerList.Count;
            if (count < 1)
                return new T[0];

            try
            {
                var result = new T[count];
                var generic = (PacketConverter<T>)converter;
                for (var i = 0; i < count; i++)
                    result[i] = generic.GetValue(readerList[i].block);
                return result;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        #endregion

        internal static T[] ToArray<T>(PacketReader reader, PacketConverter converter)
        {
            return converter.Length < 1
                ? InternalToArray<T>(reader, converter)
                : reader.block.ToArray<T>(converter);
        }

        internal static List<T> ToList<T>(PacketReader reader, PacketConverter converter)
        {
            return converter.Length < 1
                ? InternalToList<T>(reader, converter)
                : reader.block.ToList<T>(converter);
        }

        internal static Enumerable<T> ToEnumerable<T>(PacketReader reader, PacketConverter converter)
        {
            return new Enumerable<T>(reader, converter);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var builder = new DictionaryBuilder<TK, TV>();
            reader.block.ToDictionary(indexConverter, elementConverter, builder);
            return builder.dictionary;
        }

        internal static Dictionary<TK, TV> ToDictionaryExtend<TK, TV>(List<object> list)
        {
            var dictionary = new Dictionary<TK, TV>(list.Count >> 1);
            for (var i = 0; i < list.Count; i += 2)
            {
                var key = (TK)list[i];
                var value = (TV)list[i + 1];
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        internal static List<Tuple<TK, TV>> ToTupleList<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var builder = new TupleListBuilder<TK, TV>();
            reader.block.ToDictionary(indexConverter, elementConverter, builder);
            return builder.tuples;
        }

        internal static List<Tuple<TK, TV>> ToTupleListExtend<TK, TV>(List<object> list)
        {
            var tupleList = new List<Tuple<TK, TV>>(list.Count >> 1);
            for (var i = 0; i < list.Count; i += 2)
            {
                var key = (TK)list[i];
                var value = (TV)list[i + 1];
                tupleList.Add(new Tuple<TK, TV>(key, value));
            }
            return tupleList;
        }

        internal static byte[][] FromArray<T>(PacketConverter converter, T[] array)
        {
            var generic = (PacketConverter<T>)converter;
            var result = new byte[array.Length][];
            for (var i = 0; i < array.Length; i++)
                result[i] = generic.GetBytesChecked(array[i]);
            return result;
        }

        internal static byte[][] FromList<T>(PacketConverter converter, List<T> list)
        {
            var generic = (PacketConverter<T>)converter;
            var result = new byte[list.Count][];
            for (var i = 0; i < list.Count; i++)
                result[i] = generic.GetBytesChecked(list[i]);
            return result;
        }

        internal static byte[][] FromEnumerable<T>(PacketConverter converter, IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection && collection.Count > 15)
                return FromArray(converter, collection.ToArray());
            var generic = (PacketConverter<T>)converter;
            var result = new List<byte[]>();
            foreach (var i in enumerable)
                result.Add(generic.GetBytesChecked(i));
            return result.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> FromDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var capacity = enumerable is ICollection collection ? collection.Count : Extension.Capacity;
            var result = new List<KeyValuePair<byte[], byte[]>>(capacity);
            var keyGeneric = (PacketConverter<TK>)indexConverter;
            var valGeneric = (PacketConverter<TV>)elementConverter;

            foreach (var i in enumerable)
            {
                var keyBuffer = keyGeneric.GetBytesChecked(i.Key);
                var valBuffer = valGeneric.GetBytesChecked(i.Value);
                result.Add(new KeyValuePair<byte[], byte[]>(keyBuffer, valBuffer));
            }
            return result;
        }
    }
}
