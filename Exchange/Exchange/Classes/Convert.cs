using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Network
{
    internal static partial class Convert
    {
        private static List<T> InternalToList<T>(PacketReader reader, PacketConverter converter)
        {
            var readerList = reader.GetList();
            var count = readerList.Count;
            if (count < 1)
                return new List<T>();
            var result = new List<T>(count);
            var generic = converter as PacketConverter<T>;

            try
            {
                if (generic != null)
                    for (int i = 0; i < count; i++)
                        result.Add(generic.GetValue(readerList[i].element));
                else
                    for (int i = 0; i < count; i++)
                        result.Add((T)converter.GetObject(readerList[i].element));
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return result;
        }

        private static T[] InternalToArray<T>(PacketReader reader, PacketConverter converter)
        {
            var readerList = reader.GetList();
            var count = readerList.Count;
            if (count < 1)
                return new T[0];
            var result = new T[count];
            var generic = converter as PacketConverter<T>;

            try
            {
                if (generic != null)
                    for (int i = 0; i < count; i++)
                        result[i] = generic.GetValue(readerList[i].element);
                else
                    for (int i = 0; i < count; i++)
                        result[i] = (T)converter.GetObject(readerList[i].element);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return result;
        }

        internal static T[] ToArray<T>(PacketReader reader, PacketConverter converter)
        {
            return converter.Length < 1
                ? InternalToArray<T>(reader, converter)
                : reader.element.ToArray<T>(converter);
        }

        internal static List<T> ToList<T>(PacketReader reader, PacketConverter converter)
        {
            return converter.Length < 1
                ? InternalToList<T>(reader, converter)
                : reader.element.ToList<T>(converter);
        }

        internal static Enumerable<T> ToEnumerable<T>(PacketReader reader, PacketConverter converter)
        {
            return new Enumerable<T>(reader, converter);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var builder = new DictionaryBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, builder);
            return builder.dictionary;
        }

        internal static Dictionary<TK, TV> ToDictionaryExtend<TK, TV>(List<object> list)
        {
            var dictionary = new Dictionary<TK, TV>();
            var index = 0;
            while (index < list.Count)
            {
                var key = (TK)list[index++];
                var value = (TV)list[index++];
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        internal static List<Tuple<TK, TV>> ToTupleList<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var builder = new TupleListBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, builder);
            return builder.tuples;
        }

        internal static List<Tuple<TK, TV>> ToTupleListExtend<TK, TV>(List<object> list)
        {
            var tupleList = new List<Tuple<TK, TV>>();
            var index = 0;
            while (index < list.Count)
            {
                var key = (TK)list[index++];
                var value = (TV)list[index++];
                tupleList.Add(new Tuple<TK, TV>(key, value));
            }
            return tupleList;
        }

        internal static byte[][] FromArray<T>(PacketConverter converter, T[] array)
        {
            var result = new byte[array.Length][];
            if (converter is PacketConverter<T> generic)
                for (int i = 0; i < array.Length; i++)
                    result[i] = generic.GetBytesWrap(array[i]);
            else
                for (int i = 0; i < array.Length; i++)
                    result[i] = converter.GetBytesWrap(array[i]);
            return result;
        }

        internal static byte[][] FromList<T>(PacketConverter converter, List<T> list)
        {
            var result = new byte[list.Count][];
            if (converter is PacketConverter<T> generic)
                for (int i = 0; i < list.Count; i++)
                    result[i] = generic.GetBytesWrap(list[i]);
            else
                for (int i = 0; i < list.Count; i++)
                    result[i] = converter.GetBytesWrap(list[i]);
            return result;
        }

        internal static byte[][] FromEnumerable<T>(PacketConverter converter, IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection && collection.Count > 15)
                return FromArray(converter, collection.ToArray());

            var result = new List<byte[]>();
            if (converter is PacketConverter<T> generic)
                foreach (var i in enumerable)
                    result.Add(generic.GetBytesWrap(i));
            else
                foreach (var i in enumerable)
                    result.Add(converter.GetBytesWrap(i));
            return result.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> FromDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var result = new List<KeyValuePair<byte[], byte[]>>();
            var keyGeneric = indexConverter as PacketConverter<TK>;
            var valGeneric = elementConverter as PacketConverter<TV>;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var val = i.Value;
                var keyBuffer = (keyGeneric != null ? keyGeneric.GetBytesWrap(key) : indexConverter.GetBytesWrap(key));
                var valBuffer = (valGeneric != null ? valGeneric.GetBytesWrap(val) : elementConverter.GetBytesWrap(val));
                result.Add(new KeyValuePair<byte[], byte[]>(keyBuffer, valBuffer));
            }
            return result;
        }
    }
}
