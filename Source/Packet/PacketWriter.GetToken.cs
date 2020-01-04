using Mikodev.Network.Tokens;
using System.Collections;
using System.Collections.Generic;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public partial class PacketWriter
    {
        private static Token GetToken(ConverterDictionary converters, object value, int level)
        {
            PacketException.VerifyRecursionError(ref level);
            if (value == null)
                return Token.Empty;
            var type = value.GetType();
            var info = Cache.GetConverterOrInfo(converters, type, out var converter);
            return info == null ? new Value(converter.GetBytesChecked(value)) : GetTokenMatch(converters, value, level, info);
        }

        private static Token GetTokenMatch(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);
            switch (valueInfo.From)
            {
                case InfoFlags.Writer:
                    return ((PacketWriter)value).token;

                case InfoFlags.RawWriter:
                    return new Value(((PacketRawWriter)value).stream.ToArray());

                case InfoFlags.Bytes:
                    return new Value(((ICollection<byte>)value).ToBytes());

                case InfoFlags.SBytes:
                    return new Value(((ICollection<sbyte>)value).ToBytes());

                case InfoFlags.Enumerable:
                    return GetTokenEnumerable(converters, value, level, valueInfo);

                case InfoFlags.Dictionary:
                    return GetTokenDictionary(converters, value, level, valueInfo);

                case InfoFlags.Expando:
                    return GetTokenExpando(converters, value, level);

                default:
                    return GetTokenDefault(converters, value, level, valueInfo);
            }
        }

        private static Token GetTokenEnumerable(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var elementType = valueInfo.ElementType;
            var info = Cache.GetConverterOrInfo(converters, elementType, out var converter);
            if (info == null)
                return new ValueArray(valueInfo.FromEnumerable(converter, value), converter.Length);
            var list = new List<Token>();
            foreach (var i in ((IEnumerable)value))
                list.Add(GetTokenMatch(converters, i, level, info));
            return new TokenArray(list);
        }

        private static Token GetTokenDictionary(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var key = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (key == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType, valueInfo.Type);
            var elementType = valueInfo.ElementType;
            var info = Cache.GetConverterOrInfo(converters, elementType, out var converter);
            if (info == null)
                return new ValueDictionary(valueInfo.FromDictionary(key, converter, value), key.Length, converter.Length);

            var list = new List<KeyValuePair<byte[], Token>>();
            var adapter = valueInfo.FromDictionaryAdapter(key, value);
            if (valueInfo.ElementType == typeof(object))
                foreach (var i in adapter)
                    list.Add(new KeyValuePair<byte[], Token>(i.Key, GetToken(converters, i.Value, level)));
            else
                foreach (var i in adapter)
                    list.Add(new KeyValuePair<byte[], Token>(i.Key, GetTokenMatch(converters, i.Value, level, info)));
            return new TokenDictionary(list, key.Length);
        }

        private static Token GetTokenExpando(ConverterDictionary converters, object value, int level)
        {
            var dictionary = (IDictionary<string, object>)value;
            var list = new Dictionary<string, PacketWriter>(dictionary.Count);
            foreach (var i in dictionary)
                list[i.Key] = GetWriter(converters, i.Value, level);
            return new Expando(list);
        }

        private static Token GetTokenDefault(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var get = Cache.GetGetInfo(valueInfo.Type);
            var functor = get.Functor;
            var arguments = get.Arguments;
            var results = new object[arguments.Length];
            functor.Invoke(value, results);
            var dictionary = new Dictionary<string, PacketWriter>(arguments.Length);
            for (var i = 0; i < arguments.Length; i++)
                dictionary[arguments[i].Key] = GetWriter(converters, results[i], level);
            return new Expando(dictionary);
        }
    }
}
