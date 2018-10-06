using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Testing
{
    [TestClass]
    public class CollectionTest
    {
        private interface ICollectionViaAdd<T> : IEnumerable<T>
        {
            void Add(T value);
        }

        private struct ValueCollectionViaConstructor<T> : IEnumerable<T>
        {
            private readonly List<T> values;

            public ValueCollectionViaConstructor(IEnumerable<T> values) => this.values = values.ToList();

            public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
        }

        private struct ValueCollectionViaAdd<T> : ICollectionViaAdd<T>
        {
            private List<T> values;

            public void Add(T value)
            {
                if (values is null)
                    values = new List<T>();
                values.Add(value);
            }

            public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
        }

        private abstract class AbstractCollectionViaConstructor<T> : IEnumerable<T>
        {
            private readonly List<T> values;

            public AbstractCollectionViaConstructor(IEnumerable<T> values) => this.values = values.ToList();

            public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
        }

        private sealed class CollectionViaConstructor<T> : AbstractCollectionViaConstructor<T>
        {
            public CollectionViaConstructor(IEnumerable<T> values) : base(values) { }
        }

        private abstract class AbstractCollectionViaAdd<T> : ICollectionViaAdd<T>
        {
            private readonly List<T> values = new List<T>();

            protected AbstractCollectionViaAdd() { }

            public void Add(T value) => values.Add(value);

            public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
        }

        private sealed class CollectionViaAdd<T> : AbstractCollectionViaAdd<T> { }

        private const int loop = 4;

        private readonly Cache cache = new Cache();

        private readonly Random random = new Random();

        [TestMethod]
        public void StructureViaConstructor()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var anonymous = new
                {
                    c1 = new ValueCollectionViaConstructor<int>(l1),
                    c2 = new ValueCollectionViaConstructor<string>(l2),
                };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c1));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c2));

                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = PacketConvert.Deserialize(t1, anonymous);
                var r2 = cache.ToValue(t2, anonymous);

                Assert.IsTrue(l1.SequenceEqual(r1.c1));
                Assert.IsTrue(l2.SequenceEqual(r1.c2));
                Assert.IsTrue(l1.SequenceEqual(r2.c1));
                Assert.IsTrue(l2.SequenceEqual(r2.c2));
            }
        }

        [TestMethod]
        public void StructureViaAdd()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new ValueCollectionViaAdd<int>();
                foreach (var x in l1)
                    c1.Add(x);
                var c2 = new ValueCollectionViaAdd<string>();
                foreach (var x in l2)
                    c2.Add(x);
                var anonymous = new { c1, c2, };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c1));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c2));

                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = PacketConvert.Deserialize(t1, anonymous);
                var r2 = cache.ToValue(t2, anonymous);

                Assert.IsTrue(l1.SequenceEqual(r1.c1));
                Assert.IsTrue(l2.SequenceEqual(r1.c2));
                Assert.IsTrue(l1.SequenceEqual(r2.c1));
                Assert.IsTrue(l2.SequenceEqual(r2.c2));
            }
        }

        [TestMethod]
        public void ClassViaConstructor()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new CollectionViaConstructor<int>(l1);
                var c2 = new CollectionViaConstructor<string>(l2);
                var anonymous = new { c1, c2 };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c1));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c2));

                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = PacketConvert.Deserialize(t1, anonymous);
                var r2 = cache.ToValue(t2, anonymous);

                Assert.IsTrue(l1.SequenceEqual(r1.c1));
                Assert.IsTrue(l2.SequenceEqual(r1.c2));
                Assert.IsTrue(l1.SequenceEqual(r2.c1));
                Assert.IsTrue(l2.SequenceEqual(r2.c2));
            }
        }

        [TestMethod]
        public void AbstractClassViaConstructor()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new CollectionViaConstructor<int>(l1);
                var c2 = new CollectionViaConstructor<string>(l2);
                var anonymous = new
                {
                    c3 = (AbstractCollectionViaConstructor<int>)c1,
                    c4 = (AbstractCollectionViaConstructor<string>)c2
                };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c3));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c4));

                var t3 = cache.ToBytes(anonymous);
                var t4 = PacketConvert.Serialize(anonymous);

                AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue(t3, anonymous), ex => ex.Message.Contains("Unable to get collection"));
                AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize(t4, anonymous), ex => ex.ErrorCode == PacketError.InvalidType);

                var model = new { c3 = default(int[]), c4 = default(string[]), };
                var r1 = cache.ToValue(t3, model);
                var r2 = PacketConvert.Deserialize(t3, model);

                Assert.IsTrue(l1.SequenceEqual(r1.c3));
                Assert.IsTrue(l2.SequenceEqual(r1.c4));
                Assert.IsTrue(l1.SequenceEqual(r2.c3));
                Assert.IsTrue(l2.SequenceEqual(r2.c4));
            }
        }

        [TestMethod]
        public void ClassViaAdd()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new CollectionViaAdd<int>();
                foreach (var x in l1)
                    c1.Add(x);
                var c2 = new CollectionViaAdd<string>();
                foreach (var x in l2)
                    c2.Add(x);
                var anonymous = new { c1, c2, };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c1));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c2));

                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = PacketConvert.Deserialize(t1, anonymous);
                var r2 = cache.ToValue(t2, anonymous);

                Assert.IsTrue(l1.SequenceEqual(r1.c1));
                Assert.IsTrue(l2.SequenceEqual(r1.c2));
                Assert.IsTrue(l1.SequenceEqual(r2.c1));
                Assert.IsTrue(l2.SequenceEqual(r2.c2));
            }
        }

        [TestMethod]
        public void AbstractClassViaAdd()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new CollectionViaAdd<int>();
                foreach (var x in l1)
                    c1.Add(x);
                var c2 = new CollectionViaAdd<string>();
                foreach (var x in l2)
                    c2.Add(x);
                var anonymous = new
                {
                    c3 = (AbstractCollectionViaAdd<int>)c1,
                    c4 = (AbstractCollectionViaAdd<string>)c2,
                };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c3));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c4));

                var t3 = cache.ToBytes(anonymous);
                var t4 = PacketConvert.Serialize(anonymous);

                AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue(t3, anonymous), ex => ex.Message.Contains("Unable to get collection"));
                AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize(t4, anonymous), ex => ex.ErrorCode == PacketError.InvalidType);

                var model = new { c3 = default(int[]), c4 = default(string[]), };
                var r1 = cache.ToValue(t3, model);
                var r2 = PacketConvert.Deserialize(t3, model);

                Assert.IsTrue(l1.SequenceEqual(r1.c3));
                Assert.IsTrue(l2.SequenceEqual(r1.c4));
                Assert.IsTrue(l1.SequenceEqual(r2.c3));
                Assert.IsTrue(l2.SequenceEqual(r2.c4));
            }
        }

        [TestMethod]
        public void InterfaceViaAdd()
        {
            for (var i = 0; i < loop; i++)
            {
                var l1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var l2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();
                var lv1 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next()).ToList();
                var lv2 = Enumerable.Range(0, random.Next(4, 64)).Select(x => random.Next().ToString("x")).ToList();

                var c1 = new CollectionViaAdd<int>();
                foreach (var x in l1)
                    c1.Add(x);
                var c2 = new CollectionViaAdd<string>();
                foreach (var x in l2)
                    c2.Add(x);
                var v1 = new ValueCollectionViaAdd<int>();
                foreach (var x in lv1)
                    v1.Add(x);
                var v2 = new ValueCollectionViaAdd<string>();
                foreach (var x in lv2)
                    v2.Add(x);
                var anonymous = new
                {
                    c3 = (ICollectionViaAdd<int>)c1,
                    c4 = (ICollectionViaAdd<string>)c2,
                    v3 = (ICollectionViaAdd<int>)v1,
                    v4 = (ICollectionViaAdd<string>)v2,
                };
                Assert.IsTrue(l1.SequenceEqual(anonymous.c3));
                Assert.IsTrue(l2.SequenceEqual(anonymous.c4));
                Assert.IsTrue(lv1.SequenceEqual(anonymous.v3));
                Assert.IsTrue(lv2.SequenceEqual(anonymous.v4));

                var t3 = cache.ToBytes(anonymous);
                var t4 = PacketConvert.Serialize(anonymous);

                AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue(t3, anonymous), ex => ex.Message.Contains("Unable to get collection"));
                AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize(t4, anonymous), ex => ex.ErrorCode == PacketError.InvalidType);

                var model = new { c3 = default(int[]), c4 = default(string[]), v3 = default(int[]), v4 = default(string[]) };
                var r1 = cache.ToValue(t3, model);
                var r2 = PacketConvert.Deserialize(t3, model);

                Assert.IsTrue(l1.SequenceEqual(r1.c3));
                Assert.IsTrue(l2.SequenceEqual(r1.c4));
                Assert.IsTrue(lv1.SequenceEqual(r1.v3));
                Assert.IsTrue(lv2.SequenceEqual(r1.v4));
                Assert.IsTrue(l1.SequenceEqual(r2.c3));
                Assert.IsTrue(l2.SequenceEqual(r2.c4));
                Assert.IsTrue(lv1.SequenceEqual(r2.v3));
                Assert.IsTrue(lv2.SequenceEqual(r2.v4));
            }
        }

        [TestMethod]
        public void DictionaryWithObjectKey()
        {
            var dictionary = new Dictionary<object, string>();

            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(dictionary), x => x.Message.StartsWith("Invalid dictionary key type"));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue<Dictionary<object, int>>(Array.Empty<byte>()), x => x.Message.StartsWith("Invalid dictionary key type"));

            AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(dictionary), x => x.ErrorCode == PacketError.InvalidKeyType);
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<Dictionary<object, int>>(Array.Empty<byte>()), x => x.ErrorCode == PacketError.InvalidKeyType);
        }
    }
}
