using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal interface IDelegateConverter
    {
        Delegate ToBytesDelegate { get; }

        Delegate ToValueDelegate { get; }
    }
}
