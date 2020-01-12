using System;

namespace Mikodev.Network.Internal
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal sealed class ConverterAttribute : Attribute
    {
        internal Type Type { get; }

        internal ConverterAttribute(Type type) => this.Type = type;
    }
}
