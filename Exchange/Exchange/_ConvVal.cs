using System;

namespace Mikodev.Network
{
    internal sealed class _ConvVal<T> : _ConvBase<T>, IPacketConverter, IPacketConverter<T>
    {
        internal readonly int _len = 0;
        internal readonly Func<byte[], int, T> _val = null;

        internal _ConvVal(Func<T, byte[]> bin, Func<byte[], int, T> val, int len) : base(bin)
        {
            _val = val;
            _len = len;
        }

        public int? Length => _len;

        public object GetValue(byte[] buffer, int offset, int length)
        {
            try
            {
                var val = _val.Invoke(buffer, offset);
                var res = (object)val;
                return res;
            }
            catch (Exception ex) when (_Extension._Catch(ex))
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }

        T IPacketConverter<T>.GetValue(byte[] buffer, int offset, int length)
        {
            try
            {
                return _val.Invoke(buffer, offset);
            }
            catch (Exception ex) when (_Extension._Catch(ex))
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }
    }
}
