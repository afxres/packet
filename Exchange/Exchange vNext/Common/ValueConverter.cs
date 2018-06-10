using System;

namespace Mikodev.Binary.Common
{
    public abstract class ValueConverter : Converter
    {
        internal abstract Type ValueType { get; }
    }
}
