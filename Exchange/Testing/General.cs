using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Net;
using static Mikodev.Testing.Extensions;

namespace Mikodev.Testing
{
    [TestClass]
    public class General
    {
        [TestMethod]
        public void SerializeDeserialize()
        {
            var obj = new
            {
                a = 1,
                b = 2.22M,
                c = "three",
                d = new IPEndPoint(IPAddress.Loopback, 32767),
                e = new byte[] { 33, 44, 77, 88 },
                f = new[] { 1024, 32768, 1 << 24 - 1 },
                g = new[] { "one", "three", "..." },
                sub = new
                {
                    a = 0.1F,
                    b = 9.9D,
                    c = "zero",
                    d = Guid.NewGuid(),
                    e = new sbyte[] { 0, 11, -33, -55 },
                    f = new[] { 1.0F, float.MaxValue, float.MinValue },
                    g = new[] { "a", "bb", "\r\n" },
                }
            };

            var wtr = PacketWriter.Serialize(obj);
            var buf = wtr.GetBytes();
            var dir = PacketConvert.Serialize(obj);

            ThrowIfNotSequenceEqual(buf, dir);

            var rea = new PacketReader(buf);
            var val = rea.Deserialize(obj);

            Assert.AreEqual(obj.a, val.a);
            Assert.AreEqual(obj.b, val.b);
            Assert.AreEqual(obj.c, val.c);
            Assert.AreEqual(obj.d, val.d);

            Assert.AreEqual(obj.sub.a, val.sub.a);
            Assert.AreEqual(obj.sub.b, val.sub.b);
            Assert.AreEqual(obj.sub.c, val.sub.c);
            Assert.AreEqual(obj.sub.d, val.sub.d);

            ThrowIfNotSequenceEqual(obj.e, val.e);
            ThrowIfNotSequenceEqual(obj.f, val.f);
            ThrowIfNotSequenceEqual(obj.g, val.g);

            ThrowIfNotSequenceEqual(obj.sub.e, val.sub.e);
            ThrowIfNotSequenceEqual(obj.sub.f, val.sub.f);
            ThrowIfNotSequenceEqual(obj.sub.g, val.sub.g);
        }
    }
}
