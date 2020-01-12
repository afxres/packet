using Mikodev.Network.Internal;
using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    public partial class PacketReader
    {
        internal object GetValue(Type type, int level)
        {
            PacketException.VerifyRecursionError(ref level);
            var info = Cache.GetConverterOrInfo(this.converters, type, out var converter);
            return info == null
                ? converter.GetObjectChecked(this.block, true)
                : this.GetValueMatch(type, level, info);
        }

        internal object GetValueMatch(Type valueType, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);
            return valueInfo.To switch
            {
                InfoFlags.Reader => this,
                InfoFlags.RawReader => new PacketRawReader(this),
                InfoFlags.Collection => this.GetValueCollection(level, valueInfo),
                InfoFlags.Enumerable => this.GetValueEnumerable(level, valueInfo),
                InfoFlags.Dictionary => this.GetValueDictionary(level, valueInfo),
                _ => this.GetValueDefault(valueType, level),
            };
        }

        private object GetValueCollection(int level, Info valueInfo)
        {
            var info = Cache.GetConverterOrInfo(this.converters, valueInfo.ElementType, out var con);
            if (info == null)
                return valueInfo.ToCollection(this, con);
            var list = this.GetList();
            var length = list.Count;
            var source = new object[length];
            for (var i = 0; i < length; i++)
                source[i] = list[i].GetValueMatch(valueInfo.ElementType, level, info);
            var result = valueInfo.ToCollectionExtend(source);
            return result;
        }

        private object GetValueEnumerable(int level, Info valueInfo)
        {
            var info = Cache.GetConverterOrInfo(this.converters, valueInfo.ElementType, out var converter);
            return info == null
                ? valueInfo.ToEnumerable(this, converter)
                : valueInfo.ToEnumerableAdapter(this, info, level);
        }

        private object GetValueDictionary(int level, Info valueInfo)
        {
            var indexConverter = Cache.GetConverter(this.converters, valueInfo.IndexType, true);
            if (indexConverter == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType, valueInfo.Type);
            var info = Cache.GetConverterOrInfo(this.converters, valueInfo.ElementType, out var elementConverter);
            if (info == null)
                return valueInfo.ToDictionary(this, indexConverter, elementConverter);

            var collection = new List<object>();
            var vernier = (Vernier)this.block;
            while (vernier.Any)
            {
                vernier.FlushExcept(indexConverter.Length);
                // Wrap error non-check
                var key = indexConverter.GetObjectChecked(vernier.Buffer, vernier.Offset, vernier.Length);
                vernier.Flush();
                var reader = new PacketReader((Block)vernier, this.converters);
                var value = reader.GetValueMatch(valueInfo.ElementType, level, info);
                collection.Add(key);
                collection.Add(value);
            }
            return valueInfo.ToDictionaryExtend(collection);
        }

        private object GetValueDefault(Type valueType, int level)
        {
            var set = Cache.GetSetInfo(valueType);
            if (set == null)
                throw PacketException.InvalidType(valueType);
            if (this.block.Length == 0)
                return set.ThrowOrNull();
            var functor = set.Functor;
            var arguments = set.Arguments;
            var source = new object[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var reader = this.GetReader(arguments[i].Key, false);
                var result = reader.GetValue(arguments[i].Value, level);
                source[i] = result;
            }
            return functor.Invoke(source);
        }
    }
}
