using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class NullOrEmptyTest
    {
        private sealed class TestClass
        {
            public int Id { get; set; }

            public string Tag { get; set; }
        }

        private struct TestStruct
        {
            public int Id { get; set; }

            public string Tag { get; set; }
        }

        private static readonly Cache cache = new Cache();

        [TestMethod]
        public void BasicValues()
        {
            var anonymous = new
            {
                text = default(string),
                address = default(IPAddress),
                endpoint = default(IPEndPoint),
                intArray = default(int[]),
                textArray = default(string[]),
            };
            var t1 = cache.ToBytes(anonymous);
            var t2 = PacketConvert.Serialize(anonymous);
            var r1 = PacketConvert.Deserialize(t1, anonymous);
            var r2 = cache.ToValue(t2, anonymous);

            Assert.IsFalse(ReferenceEquals(anonymous, r1));
            Assert.IsFalse(ReferenceEquals(anonymous, r2));

            Assert.AreEqual(r1.text, string.Empty);
            Assert.AreEqual(r1.address, null);
            Assert.AreEqual(r1.endpoint, null);
            Assert.AreEqual(r1.intArray.Length, 0);
            Assert.AreEqual(r1.textArray.Length, 0);

            Assert.AreEqual(r2.text, string.Empty);
            Assert.AreEqual(r2.address, null);
            Assert.AreEqual(r2.endpoint, null);
            Assert.AreEqual(r2.intArray.Length, 0);
            Assert.AreEqual(r2.textArray.Length, 0);
        }

        [TestMethod]
        public void Collection()
        {
            var anonymous = new
            {
                intArray = default(int[]),
                stringArray = default(string[]),
                intEnumerable = default(IEnumerable<int>),
                stringCollection = default(ICollection<string>),
                intList = default(List<int>),
                stringIList = default(IList<string>),
                intSet = default(HashSet<int>),
                stringISet = default(ISet<string>),

                dictionary = default(Dictionary<int, string>),
                idictionary = default(IDictionary<string, int>),
            };
            var t1 = cache.ToBytes(anonymous);
            var t2 = PacketConvert.Serialize(anonymous);
            var r1 = PacketConvert.Deserialize(t1, anonymous);
            var r2 = cache.ToValue(t2, anonymous);

            Assert.IsFalse(ReferenceEquals(anonymous, r1));
            Assert.IsFalse(ReferenceEquals(anonymous, r2));

            Assert.AreEqual(r1.intArray.Length, 0);
            Assert.AreEqual(r1.stringArray.Length, 0);
            Assert.AreEqual(r1.intEnumerable.Count(), 0);
            Assert.AreEqual(r1.stringCollection.Count, 0);
            Assert.AreEqual(r1.intList.Count, 0);
            Assert.AreEqual(r1.stringIList.Count, 0);
            Assert.AreEqual(r1.intSet.Count, 0);
            Assert.AreEqual(r1.stringISet.Count, 0);
            Assert.AreEqual(r1.dictionary.Count, 0);
            Assert.AreEqual(r1.idictionary.Count, 0);

            Assert.AreEqual(r2.intArray.Length, 0);
            Assert.AreEqual(r2.stringArray.Length, 0);
            Assert.AreEqual(r2.intEnumerable.Count(), 0);
            Assert.AreEqual(r2.stringCollection.Count, 0);
            Assert.AreEqual(r2.intList.Count, 0);
            Assert.AreEqual(r2.stringIList.Count, 0);
            Assert.AreEqual(r2.intSet.Count, 0);
            Assert.AreEqual(r2.stringISet.Count, 0);
            Assert.AreEqual(r2.dictionary.Count, 0);
            Assert.AreEqual(r2.idictionary.Count, 0);
        }

        [TestMethod]
        public void Tuple()
        {
            // default value is null
            var v1 = default(Tuple<string, int>);
            var t1 = cache.ToBytes(v1);
            Assert.IsTrue(t1.Length == 0);
            var r1 = cache.ToValue<Tuple<string, int>>(Array.Empty<byte>());
            Assert.AreEqual(r1, null);

            // default value not null
            var v2 = default((int, string));
            var t2 = cache.ToBytes(v2);
            Assert.IsTrue(t2.Length != 0);
            AssertExtension.MustFail<OverflowException>(() => cache.ToValue<(int, string)>(Array.Empty<byte>()));

            var anonymous = new
            {
                tuple = default(Tuple<int, string>),
                value = default((string text, int)),
            };
            var t3 = cache.ToBytes(anonymous);
            var r3 = cache.ToValue(t3, anonymous);

            Assert.IsFalse(ReferenceEquals(r3, anonymous));
            Assert.IsTrue(r3.tuple == null);
            Assert.IsTrue(r3.value.text == string.Empty);
            AssertExtension.MustFail<OverflowException>(() => cache.ToValue(t3, new { tuple = default((int, string)) }));
        }

        [TestMethod]
        public void Class()
        {
            var source = default(TestClass);
            var t1 = cache.ToBytes(source);
            var t2 = PacketConvert.Serialize(source);
            Assert.IsTrue(t1.Length == 0);
            Assert.IsTrue(t2.Length == 0);

            var r1 = cache.ToValue<TestClass>(Array.Empty<byte>());
            var r2 = PacketConvert.Deserialize<TestClass>(Array.Empty<byte>());
            Assert.IsTrue(r1 == null);
            Assert.IsTrue(r2 == null);
        }

        [TestMethod]
        public void ValueType()
        {
            var source = default(TestStruct);
            var t1 = cache.ToBytes(source);
            var t2 = PacketConvert.Serialize(source);
            Assert.IsTrue(t1.Length != 0);
            Assert.IsTrue(t2.Length != 0);

            AssertExtension.MustFail<OverflowException>(() => cache.ToValue<TestStruct>(Array.Empty<byte>()));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<TestStruct>(Array.Empty<byte>()), x => x.ErrorCode == PacketError.Overflow);
        }
    }
}
