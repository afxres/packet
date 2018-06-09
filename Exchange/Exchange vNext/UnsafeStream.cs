using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal sealed class UnsafeStream
    {
        internal readonly struct VerifyResult
        {
            internal readonly byte[] buffer;
            internal readonly int offset;

            internal VerifyResult(byte[] buffer, int offset)
            {
                this.buffer = buffer;
                this.offset = offset;
            }
        }

        private const int InitialLength = 256;
        private const int MaximumLength = 0x4000_0000;

        private byte[] stream = new byte[InitialLength];
        private int position;

        internal VerifyResult VerifyAvailable(int require)
        {
            if ((uint)require > MaximumLength)
                goto fail;
            var offset = position;
            var buffer = stream;
            if (buffer.Length - offset < require)
            {
                long limits = offset + require;
                long length = buffer.Length;
                do
                {
                    length <<= 2;
                    if (length > MaximumLength)
                        goto fail;
                }
                while (length < limits);
                var target = new byte[(int)length];
                Unsafe.CopyBlockUnaligned(ref target[0], ref buffer[0], (uint)length);
                stream = target;
                buffer = target;
            }
            position = offset + require;
            return new VerifyResult(buffer, offset);

            fail:
            throw new OverflowException();
        }
    }
}
