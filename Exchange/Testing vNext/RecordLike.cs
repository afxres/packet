using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class RecordLike
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
                return this.Id == other.Id &&
                       this.Name == other.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + this.Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
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
                       this.Guid.Equals(other.Guid) &&
                       EqualityComparer<IPEndPoint>.Default.Equals(this.EndPoint, other.EndPoint);
            }

            public override int GetHashCode()
            {
                var hashCode = -1964891576;
                hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(this.Guid);
                hashCode = hashCode * -1521134295 + EqualityComparer<IPEndPoint>.Default.GetHashCode(this.EndPoint);
                return hashCode;
            }
        }

        private static Cache cache = new Cache();

        [TestMethod]
        public void Structure()
        {
            var value = new SimpleStructure(int.MaxValue, "sharp");
            var t1 = cache.Serialize(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<SimpleStructure>(t1);
            var r2 = cache.Deserialize<SimpleStructure>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }

        [TestMethod]
        public void Class()
        {
            var value = new SimpleClass(Guid.NewGuid(), new IPEndPoint(IPAddress.Loopback, 3389)); var t1 = cache.Serialize(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = PacketConvert.Deserialize<SimpleClass>(t1);
            var r2 = cache.Deserialize<SimpleClass>(t2);

            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }
    }
}
