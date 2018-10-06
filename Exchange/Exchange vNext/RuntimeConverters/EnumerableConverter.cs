using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class EnumerableConverter<TE, TV> : Converter<TE>, IEnumerableConverter where TE : IEnumerable<TV>
    {
        private readonly Converter<TV> converter;

        private readonly Func<IEnumerable<TV>, TE> toValue;

        public EnumerableConverter(Converter<TV> converter, Func<IEnumerable<TV>, TE> toValue) : base(0)
        {
            this.converter = converter;
            this.toValue = toValue;
        }

        public Delegate GetToEnumerableDelegate() => toValue;

        public override void ToBytes(Allocator allocator, TE value)
        {
            if (value == null)
                return;
            if (converter.length == 0)
                foreach (var i in value)
                    allocator.AppendValueExtend(converter, i);
            else
                foreach (var i in value)
                    converter.ToBytes(allocator, i);
        }

        public override TE ToValue(ReadOnlyMemory<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get collection, type: {typeof(TE)}");
            var enumerable = converter.length == 0
                ? (IEnumerable<TV>)ListConverter<TV>.Value(memory, converter)
                : ArrayConverter<TV>.Value(memory, converter);
            return toValue.Invoke(enumerable);
        }
    }
}
