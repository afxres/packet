using System;
using System.Diagnostics;

namespace Mikodev.Test
{
    internal class TraceWatch : IDisposable
    {
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
            Console.WriteLine($"[{_watch.ElapsedMilliseconds} ms] {_msg}");
        }
    }
}
