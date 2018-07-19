using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace Mikodev.Testing
{
    internal static class AssertExtension
    {
        public static void MustFail<E>(Action action, Func<E, bool> filter = null) where E : Exception
        {
            try
            {
                action.Invoke();
                Assert.Fail();
            }
            catch (E ex) when (filter == null || filter.Invoke(ex))
            {
                Trace.WriteLine($"[Must fail] exception type : {typeof(E)}, message : {ex.Message}");
            }
        }
    }
}
