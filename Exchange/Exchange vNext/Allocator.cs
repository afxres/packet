using System;

namespace Mikodev.Binary
{
    public readonly struct Allocator
    {
        private readonly UnsafeStream stream;

        internal Allocator(UnsafeStream stream) => this.stream = stream;

        public Span<byte> Allocate(int length)
        {
            if (stream == null)
                throw new InvalidOperationException();
            var result = stream.VerifyAvailable(length);
            return new Span<byte>(result.buffer, result.offset, length);
        }

        public ref byte UnsafeAllocate(int length)
        {
            if (stream == null)
                throw new InvalidOperationException();
            var result = stream.VerifyAvailable(length);
            return ref result.buffer[result.offset];
        }
    }
}
