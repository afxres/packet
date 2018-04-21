using System;

namespace Mikodev.Network.Converters
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class PacketConverterAttribute : Attribute
    {
        private readonly Type type;

        internal Type Type => type;

        internal PacketConverterAttribute(Type type) => this.type = type;
    }
}
