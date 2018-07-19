using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class AbstractType
    {
        private interface IObject
        {
            string Id { get; }
        }

        private abstract class AbstractObject : IObject
        {
            public string Id { get; }

            public AbstractObject(string Id) => this.Id = Id;
        }

        private sealed class SimpleClass : AbstractObject
        {
            public Guid Guid { get; }

            public SimpleClass(string Id, Guid Guid) : base(Id) => this.Guid = Guid;
        }

        private readonly Cache cache = new Cache();

        [TestMethod]
        public void Abstract()
        {
            var value = (AbstractObject)new SimpleClass("2048", Guid.NewGuid());
            var t1 = cache.Serialize(value);
            var t2 = PacketConvert.Serialize(value);
            var token = cache.NewToken(t1);
            var reader = new PacketReader(t2);

            AssertExtension.MustFail<InvalidOperationException>(() => cache.Deserialize<AbstractObject>(t1));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<AbstractObject>(t2));


            Assert.AreEqual(value.Id, reader["Id"].GetValue<string>());
            Assert.AreEqual(((SimpleClass)value).Guid, reader["Guid"].GetValue<Guid>());

            Assert.AreEqual(value.Id, token["Id"].As<string>());
            AssertExtension.MustFail<KeyNotFoundException>(() => token["Guid"].As<Guid>());
        }

        [TestMethod]
        public void Interface()
        {
            var value = (IObject)new SimpleClass("2048", Guid.NewGuid());
            var t1 = cache.Serialize(value);
            var t2 = PacketConvert.Serialize(value);
            var token = cache.NewToken(t1);
            var reader = new PacketReader(t2);

            AssertExtension.MustFail<InvalidOperationException>(() => cache.Deserialize<IObject>(t1));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<IObject>(t2));


            Assert.AreEqual(value.Id, reader["Id"].GetValue<string>());
            Assert.AreEqual(((SimpleClass)value).Guid, reader["Guid"].GetValue<Guid>());

            Assert.AreEqual(value.Id, token["Id"].As<string>());
            AssertExtension.MustFail<KeyNotFoundException>(() => token["Guid"].As<Guid>());
        }

    }
}
