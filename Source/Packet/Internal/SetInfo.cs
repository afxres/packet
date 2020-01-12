using System;
using System.Collections.Generic;

namespace Mikodev.Network.Internal
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
            this.valueType = type.IsValueType;
            this.Functor = functor;
            this.Arguments = arguments;
        }

        internal object ThrowOrNull()
        {
            if (this.valueType)
                throw PacketException.Overflow();
            return null;
        }
    }
}
