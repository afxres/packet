using Mikodev.Network.Internal;
using Mikodev.Network.Tokens;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter : IDynamicMetaObjectProvider
    {
        internal readonly ConverterDictionary converters;

        internal Token token;

        internal PacketWriter(ConverterDictionary converters, Token token)
        {
            this.converters = converters;
            this.token = token;
        }

        internal PacketWriter(ConverterDictionary converters, PacketWriter writer)
        {
            this.converters = converters;
            this.token = (writer != null ? writer.token : Token.Empty);
        }

        public PacketWriter(ConverterDictionary converters = null)
        {
            this.converters = converters;
            this.token = Token.Empty;
        }

        internal IEnumerable<string> GetKeys()
        {
            var token = this.token;
            return token is Expando data
                ? data.data.Keys
                : System.Linq.Enumerable.Empty<string>();
        }

        internal Dictionary<string, PacketWriter> GetDictionary()
        {
            var token = this.token;
            if (token is Expando data)
                return data.data;
            var dictionary = new Dictionary<string, PacketWriter>(Extension.Capacity);
            this.token = new Expando(dictionary);
            return dictionary;
        }

        internal static PacketWriter GetWriter(ConverterDictionary converters, object value, int level)
        {
            return new PacketWriter(converters, GetToken(converters, value, level));
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicWriter(parameter, this);

        public byte[] GetBytes()
        {
            var token = this.token;
            var data = token.Data;
            if (data == null)
                return Internal.Empty.Array<byte>();
            if (data is byte[] bytes)
                return bytes;
            var allocator = new Allocator();
            token.FlushTo(allocator, 0);
            return allocator.GetBytes();
        }

        public override string ToString()
        {
            var data = this.token.Data;
            return data is byte[] bytes
                ? $"{nameof(PacketWriter)}(Bytes: {bytes.Length})"
                : $"{nameof(PacketWriter)}(Nodes: {(data as ICollection)?.Count ?? 0})";
        }

        public static PacketWriter Serialize(object value, ConverterDictionary converters = null) => GetWriter(converters, value, 0);
    }
}
