using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class SetInfo
    {
        private readonly Type type;

        private readonly bool valueType;

        internal Func<object[], object> Functor { get; }

        internal KeyValuePair<string, Type>[] Arguments { get; }

        internal SetInfo(Type type, Func<object[], object> functor, KeyValuePair<string, Type>[] arguments)
        {
            this.type = type;
            valueType = type.IsValueType;
            Functor = functor;
            Arguments = arguments;
        }

        internal object ThrowOrNull()
        {
            if (valueType)
                throw PacketException.Overflow();
            return null;
        }
    }
}
