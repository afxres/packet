using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class EnumerableConverter<T, TItem> : Converter<T>, IEnumerableConverter where T : IEnumerable<TItem>
    {
        private readonly Converter<TItem> converter;

        private readonly Func<IEnumerable<TItem>, T> toValue;

        public EnumerableConverter(Converter<TItem> converter, Func<IEnumerable<TItem>, T> toValue) : base(0)
        {
            this.converter = converter;
            this.toValue = toValue;
        }

        public Delegate GetToEnumerableDelegate() => toValue;

        public override void ToBytes(ref Allocator allocator, T value)
        {
            if (value == null)
                return;
            if (converter.length == 0)
                foreach (var i in value)
                    allocator.AppendValueExtend(converter, i);
            else
                foreach (var i in value)
                    converter.ToBytes(ref allocator, i);
        }

        public override T ToValue(ReadOnlySpan<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get collection, type: {typeof(T)}");
            var enumerable = converter.length == 0
                ? (IEnumerable<TItem>)ListConverter<TItem>.Value(memory, converter)
                : ArrayConverter<TItem>.Value(memory, converter);
            return toValue.Invoke(enumerable);
        }
    }
}
