using System;
using System.Diagnostics;
// using System.Threading.Tasks;

namespace Mikodev.Test
{
    internal class TraceWatch : IDisposable
    {
        internal static Action<string, TimeSpan> InstanceDisposed = null;

        internal readonly string _msg = null;
        internal readonly Stopwatch _watch = new Stopwatch();

        internal TraceWatch()
        {
            _watch.Start();
        }

        internal TraceWatch(string message)
        {
            _msg = message;
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            //Task.Factory.FromAsync(
            //    (tag, span, callback, obj) => InstanceDisposed.BeginInvoke(tag, span, callback, obj),
            //    (iasync) => InstanceDisposed.EndInvoke(iasync),
            //    _msg, _watch.Elapsed, null);
            InstanceDisposed.Invoke(_msg, _watch.Elapsed);
        }
    }
}
