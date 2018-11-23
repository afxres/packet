using System;

namespace Mikodev.Network
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal sealed class ConverterAttribute : Attribute
    {
        internal Type Type { get; }

        internal ConverterAttribute(Type type) => Type = type;
    }
}
