using System;

namespace Mikodev.Network.Converters
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class _ConverterAttribute : Attribute
    {
        private readonly Type _type;

        internal Type Type => _type;

        internal _ConverterAttribute(Type type) => _type = type;
    }
}
