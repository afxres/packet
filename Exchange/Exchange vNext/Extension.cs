using System;

namespace Mikodev.Binary
{
    internal static class Extension
    {
        internal static object Invoke<R>(Func<object, R> func, Type[] types, object[] parameters)
        {
            var delegateMethodInfo = func.Method;
            var methodInfo = delegateMethodInfo.GetGenericMethodDefinition().MakeGenericMethod(types);
            var result = methodInfo.Invoke(func.Target, parameters);
            return result;
        }
    }
}
