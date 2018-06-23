using System;

namespace Mikodev.Binary
{
    internal abstract class Adapter
    {
        public abstract Delegate BytesDelegate { get; }

        public abstract Delegate ValueDelegate { get; }
    }
}
