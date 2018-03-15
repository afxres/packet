using System;

namespace Mikodev.Network.Converters
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class PacketConverterAttribute : Attribute
    {
        private readonly Type _type;

        internal Type Type => _type;

        internal PacketConverterAttribute(Type type) => _type = type;
    }
}
