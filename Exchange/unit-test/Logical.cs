using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections.Generic;

namespace Mikodev.UnitTest
{
    internal class _Ref { }

    internal class _Converter : IPacketConverter
    {
        internal static readonly string _BytesErr = Guid.NewGuid().ToString();

        internal static readonly string _ValueErr = Guid.NewGuid().ToString();

        int IPacketConverter.Length => 0;

        byte[] IPacketConverter.GetBytes(object value) => throw new Exception(_BytesErr);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => throw new OutOfMemoryException(_ValueErr);
    }

    [TestClass]
    public class Logical
    {
        [TestMethod]
        public void Rethrow()
        {
            var val = new _Ref();
            var con = new Dictionary<Type, IPacketConverter> { [typeof(_Ref)] = new _Converter() };

            try
            {
                var buf = new PacketRawWriter(con).Push(val);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConvertError && ex.InnerException.Message == _Converter._BytesErr)
            {
                // ignore
            }

            try
            {
                var res = new PacketRawReader(new byte[4], con).Pull<_Ref>();
            }
            catch (Exception ex) when (ex.Message == _Converter._ValueErr)
            {
                // ignore
            }
        }

        [TestMethod]
        public void SpecialSerialize()
        {
            var msg = "Hello, exception!";
            var e = new Exception(msg);
            var pkt = PacketWriter.Serialize(e);
            var buf = pkt.GetBytes();

            var rea = new PacketReader(buf);
            Assert.AreEqual(msg, rea[nameof(Exception.Message)].Pull<string>());
        }
    }
}
