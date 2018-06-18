using Mikodev.Binary.Common;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class EnumerableConverter<TE, TV> : Converter<TE> where TE : IEnumerable<TV>
    {
        private readonly Converter<TV> converter;
        private readonly Func<TV[], TE> toValue;

        public EnumerableConverter(Converter<TV> converter, Func<TV[], TE> toValue) : base(0)
        {
            this.converter = converter;
            this.toValue = toValue;
        }

        public override void ToBytes(Allocator allocator, TE value)
        {
            if (value == null)
                return;
            if (value is ICollection<TV> collection)
            {
                var count = collection.Count;
                if (count == 0)
                    return;
                var array = new TV[count];
                collection.CopyTo(array, 0);
                ArrayConverter<TV>.ToBytes(allocator, array, converter);
            }
            else if (converter.Length == 0)
            {
                int offset;
                var stream = allocator.stream;
                foreach (var i in value)
                {
                    offset = stream.BeginModify();
                    converter.ToBytes(allocator, i);
                    stream.EndModify(offset);
                }
            }
            else
            {
                foreach (var i in value)
                {
                    converter.ToBytes(allocator, i);
                }
            }
        }

        public override TE ToValue(Block block)
        {
            if (toValue == null)
                throw new InvalidOperationException();
            var array = ArrayConverter<TV>.ToValue(block, converter);
            return toValue.Invoke(array);
        }
    }
}
