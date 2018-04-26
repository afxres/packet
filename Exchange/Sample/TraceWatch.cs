using System;
using System.Diagnostics;
// using System.Threading.Tasks;

namespace Mikodev.Test
{
    internal class TraceWatch : IDisposable
    {
        internal static Action<string, TimeSpan> InstanceDisposed = null;

        internal readonly string message = null;
        internal readonly Stopwatch watch = new Stopwatch();

        internal TraceWatch()
        {
            watch.Start();
        }

        internal TraceWatch(string message)
        {
            this.message = message;
            watch.Start();
        }

        public void Dispose()
        {
            watch.Stop();
            InstanceDisposed?.Invoke(message, watch.Elapsed);
        }
    }
}
