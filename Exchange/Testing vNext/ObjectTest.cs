using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class ObjectTest
    {
        private readonly Cache cache = new Cache();

        [TestMethod]
        public void Object()
        {
            AssertExtension.MustFail<InvalidOperationException>(() => cache.Serialize(new object()), x => x.Message.Contains("Invalid type"));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(new object()), x => x.ErrorCode == PacketError.InvalidType);
        }

        [TestMethod]
        public void ObjectValue()
        {
            var anonymousOther = new
            {
                valueItem = double.MaxValue,
                classItem = IPAddress.Loopback,
            };

            var anonymousSharp = new
            {
                boxedObject = (object)float.MinValue,
                classObject = (object)new IPEndPoint(IPAddress.IPv6Loopback, 0xAAFF),
            };

            var anonymous = new
            {
                normal = new
                {
                    emptyObject = default(object),
                    boxedObject = (object)int.MaxValue,
                    classObject = (object)"some",
                },
                other = (object)anonymousOther,
                sharp = (object)anonymousSharp,
            };

            var t1 = cache.Serialize(anonymous);
            var t2 = PacketConvert.Serialize(anonymous);
            var r1 = PacketConvert.Deserialize(t1, anonymous);
            var r2 = cache.Deserialize(t2, anonymous);

            Assert.IsFalse(ReferenceEquals(anonymous, r1));
            Assert.IsFalse(ReferenceEquals(anonymous, r2));

            var n1 = r1.normal;
            var n2 = r2.normal;

            Assert.AreEqual(((byte[])(dynamic)n1.emptyObject).Length, 0);
            Assert.AreEqual((string)(dynamic)n1.emptyObject, string.Empty);
            Assert.AreEqual((IPAddress)(dynamic)n1.emptyObject, null);

            Assert.AreEqual(anonymous.normal.boxedObject, (int)(dynamic)n1.boxedObject);
            Assert.AreEqual(anonymous.normal.classObject, (string)(dynamic)n1.classObject);

            Assert.AreEqual(((byte[])(dynamic)n2.emptyObject).Length, 0);
            Assert.AreEqual((string)(dynamic)n2.emptyObject, string.Empty);
            Assert.AreEqual((IPAddress)(dynamic)n2.emptyObject, null);

            Assert.AreEqual(anonymous.normal.boxedObject, (int)(dynamic)n2.boxedObject);
            Assert.AreEqual(anonymous.normal.classObject, (string)(dynamic)n2.classObject);

            var m1 = ((PacketReader)r1.other).Deserialize(anonymousOther);
            var m2 = ((Token)r2.other).As(anonymousOther);

            Assert.IsFalse(ReferenceEquals(anonymousOther, m1));
            Assert.IsFalse(ReferenceEquals(anonymousOther, m2));

            Assert.AreEqual(anonymous.other, m1);
            Assert.AreEqual(anonymous.other, m2);

            var s1 = (dynamic)r1.sharp;
            var s2 = (dynamic)r2.sharp;

            Assert.AreEqual(anonymousSharp.boxedObject, (float)s1.boxedObject);
            Assert.AreEqual(anonymousSharp.classObject, (IPEndPoint)s1.classObject);

            Assert.AreEqual(anonymousSharp.boxedObject, (float)s2.boxedObject);
            Assert.AreEqual(anonymousSharp.classObject, (IPEndPoint)s2.classObject);
        }

        [TestMethod]
        public void ObjectCollection()
        {
            void Assert<T>(T item)
            {
                AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(item), x => x.ErrorCode == PacketError.InvalidElementType);
                AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<T>(Array.Empty<byte>()), x => x.ErrorCode == PacketError.InvalidElementType);

                AssertExtension.MustFail<InvalidOperationException>(() => cache.Serialize(item), x => x.Message.Contains("Invalid collection"));
                AssertExtension.MustFail<InvalidOperationException>(() => cache.Deserialize<T>(Array.Empty<byte>()), x => x.Message.Contains("Invalid collection"));
            }

            Assert(new object[] { });
            Assert(new List<object> { });
            Assert((IList<object>)new List<object> { });
            Assert(new HashSet<object> { });
            Assert((ISet<object>)new HashSet<object> { });
        }
    }
}
