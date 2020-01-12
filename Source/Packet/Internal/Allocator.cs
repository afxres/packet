using Mikodev.Network.Converters;
using Mikodev.Network.Tokens;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Internal
{
    internal sealed class Allocator
    {
        /* 警告: 该类仅应用于单线程环境 */

        private const int InitialLength = 256;

        private const int MaximumLength = 0x4000_0000;

        private byte[] buffer = new byte[InitialLength];

        private int position = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReAllocate(int offset, int require)
        {
            if ((uint)require > MaximumLength)
                goto fail;
            long limits = offset + require;
            long length = this.buffer.Length;
            do
            {
                length <<= 2;
                if (length > MaximumLength)
                    goto fail;
            }
            while (length < limits);
            var target = new byte[(int)length];
            Unsafe.Copy(ref target[0], in this.buffer[0], offset);
            this.buffer = target;
            return;

        fail:
            throw PacketException.Overflow();
        }

        private int Allocate(int require)
        {
            var offset = this.position;
            if ((uint)require > (uint)(this.buffer.Length - offset))
                this.ReAllocate(offset, require);
            this.position = offset + require;
            return offset;
        }

        internal void Append(byte[] source)
        {
            if (source.Length == 0)
                return;
            var offset = this.Allocate(source.Length);
            Unsafe.Copy(ref this.buffer[offset], in source[0], source.Length);
        }

        internal void AppendValueExtend(byte[] source)
        {
            var offset = this.Allocate(source.Length + sizeof(int));
            UnmanagedValueConverter<int>.UnsafeToBytes(ref this.buffer[offset], source.Length);
            if (source.Length == 0)
                return;
            Unsafe.Copy(ref this.buffer[offset + sizeof(int)], in source[0], source.Length);
        }

        internal void AppendKey(string key) => this.AppendValueExtend(PacketConvert.Encoding.GetBytes(key));

        internal void AppendTokenExtend(Token token, int level)
        {
            PacketException.VerifyRecursionError(ref level);
            var offset = this.Allocate(sizeof(int));
            token.FlushTo(this, level);
            UnmanagedValueConverter<int>.UnsafeToBytes(ref this.buffer[offset], (this.position - offset - sizeof(int)));
        }

        internal byte[] GetBytes()
        {
            var length = this.position;
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            Unsafe.Copy(ref target[0], in this.buffer[0], length);
            return target;
        }
    }
}
