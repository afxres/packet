using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal interface IDelegateConverter
    {
        Delegate ToBytesFunction { get; }
        Delegate ToValueFunction { get; }
    }
}
