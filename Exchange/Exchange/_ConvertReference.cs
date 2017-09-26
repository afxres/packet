using System;

namespace Mikodev.Network
{
    internal sealed class _ConvertReference<T> : _ConvertBase<T>, IPacketConverter<T>
    {
        internal readonly Func<byte[], int, int, T> _val = null;

        internal _ConvertReference(Func<T, byte[]> bin, Func<byte[], int, int, T> val) : base(bin) => _val = val;

        public int? Length => null;

        public object GetValue(byte[] buffer, int offset, int length)
        {
            try
            {
                var val = _val.Invoke(buffer, offset, length);
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
                return _val.Invoke(buffer, offset, length);
            }
            catch (Exception ex) when (_Extension._Catch(ex))
            {
                throw new PacketException(PacketError.ConvertError, ex);
            }
        }
    }
}
