using System;

namespace Mikodev.Network
{
    internal class _ConvBase<T>
    {
        internal readonly Func<T, byte[]> _bin = null;

        internal _ConvBase(Func<T, byte[]> bin) => _bin = bin;

        public byte[] GetBytes(object value)
        {
            try
            {
                return _bin.Invoke((T)value); ;
            }
            catch (Exception ex) when (_Extension._Catch(ex))
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }

        public byte[] GetBytes(T value)
        {
            try
            {
                return _bin.Invoke(value);
            }
            catch (Exception ex) when (_Extension._Catch(ex))
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }
    }
}
