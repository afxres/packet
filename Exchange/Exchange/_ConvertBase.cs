using System;

namespace Mikodev.Network
{
    internal class _ConvertBase<T>
    {
        internal readonly Func<T, byte[]> _bin = null;

        internal _ConvertBase(Func<T, byte[]> bin) => _bin = bin;

        internal void _Raise(Exception ex)
        {
            if (ex._IsCritical())
                return;
            throw new PacketException(PacketError.ConvertError, ex);
        }

        public byte[] GetBytes(object value)
        {
            try
            {
                return _bin.Invoke((T)value); ;
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }

        public byte[] GetBytes(T value)
        {
            try
            {
                return _bin.Invoke(value);
            }
            catch (Exception ex)
            {
                _Raise(ex);
                throw;
            }
        }
    }
}
