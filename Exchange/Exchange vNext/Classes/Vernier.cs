﻿using Mikodev.Binary.Converters;
using System;

namespace Mikodev.Binary
{
    internal struct Vernier
    {
        private readonly byte[] buffer;
        private readonly int limits;
        private int offset;
        private int length;

        internal byte[] Buffer => buffer;

        internal int Offset => offset;

        internal int Length => length;

        internal bool Any => limits - offset != length;

        internal Vernier(Block block)
        {
            buffer = block.Buffer;
            offset = block.Offset;
            limits = block.Offset + block.Length;
            length = 0;
        }

        internal void Flush()
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                goto fail;
            var length = UnmanagedValueConverter<int>.ToValueUnchecked(ref buffer[offset]);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                goto fail;
            this.length = length;
            return;

            fail:
            throw new OverflowException();
        }

        internal void FlushExcept(int define)
        {
            if (define > 0)
            {
                offset += length;
                if ((uint)(limits - offset) < (uint)define)
                    throw new OverflowException();
                length = define;
            }
            else
            {
                Flush();
            }
        }
    }
}