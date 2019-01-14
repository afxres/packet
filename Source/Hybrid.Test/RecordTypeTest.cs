using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class RecordTypeTest
    {
        private struct SimpleStructure : IEquatable<SimpleStructure>
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public SimpleStructure(int Id, string Name)
            {
                this.Id = Id;
                this.Name = Name;
            }

            public override bool Equals(object obj)
            {
                return obj is SimpleStructure && Equals((SimpleStructure)obj);
            }

            public bool Equals(SimpleStructure other)
            {
                return Id == other.Id &&
                       Name == other.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        private sealed class SimpleClass : IEquatable<SimpleClass>
        {
            public Guid Guid { get; set; }

            public IPEndPoint EndPoint { get; set; }

            public SimpleClass(Guid Guid, IPEndPoint EndPoint)
            {
                this.Guid = Guid;
                this.EndPoint = EndPoint;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as SimpleClass);
            }

            public bool Equals(SimpleClass other)
            {
                return other != null &&
                       Guid.Equals(other.Guid) &&
                       EqualityComparer<IPEndPoint>.Default.Equals(EndPoint, other.EndPoint);
            }

            public override int GetHashCode()
            {
                var hashCode = -1964891576;
                hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
                hashCode = hashCode * -1521134295 + EqualityComparer<IPEndPoint>.Default.GetHashCode(EndPoint);
                return hashCode;
            }
        }

        private readonly struct TestStructure : IEquatable<TestStructure>
        {
            public string Id { get; }

            public double Item { get; }

            public TestStructure(string id, double item) : this()
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Item = item;
            }

            public override bool Equals(object obj) => obj is TestStructure && Equals((TestStructure)obj);

            public bool Equals(TestStructure other) => Id == other.Id && Item == other.Item;

            public override int GetHashCode()
            {
                var hashCode = -1659428602;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + Item.GetHashCode();
                return hashCode;
            }
        }

        private sealed class TestClass : IEquatable<TestClass>
        {
            public string Name { get; }

            public string Test { get; }

            public TestClass(string name, string test)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Test = test ?? throw new ArgumentNullException(nameof(test));
            }

            public override bool Equals(object obj) => Equals(obj as TestClass);

            public bool Equals(TestClass other) => other != null && Name == other.Name && Test == other.Test;

            public override int GetHashCode()
            {
                var hashCode = 1344153595;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Test);
                return hashCode;
            }
        }

        private static Generator generator = new Generator();

        [TestMethod]
        public void Structure()
        {
            var value = new SimpleStructure(int.MaxValue, "sharp");
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<SimpleStructure>(t1);
            var r2 = generator.ToValue<SimpleStructure>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }

        [TestMethod]
        public void Class()
        {
            var value = new SimpleClass(Guid.NewGuid(), new IPEndPoint(IPAddress.Loopback, 3389));
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<SimpleClass>(t1);
            var r2 = generator.ToValue<SimpleClass>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }

        [TestMethod]
        public void StructureInformal()
        {
            var value = new TestStructure("world", 2.71);
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<TestStructure>(t1);
            var r2 = generator.ToValue<TestStructure>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }

        [TestMethod]
        public void ClassInformal()
        {
            var value = new TestClass("hello", "The quick...");
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<TestClass>(t1);
            var r2 = generator.ToValue<TestClass>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }
    }
}
