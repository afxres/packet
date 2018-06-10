using System;

namespace Mikodev.Binary.Common
{
    public abstract class VariableValueConverter<T> : ValueConverter<T>
    {
        internal sealed override Type ValueType => typeof(T);

        protected VariableValueConverter() : base(0) { }

        public abstract void ToBytes(Allocator allocator, T value);
        
        internal sealed override void ToBytes(Allocator allocator, object @object) => ToBytes(allocator, (T)@object);
    }
}
