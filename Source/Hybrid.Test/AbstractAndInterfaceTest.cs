using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class AbstractAndInterfaceTest
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

        private readonly Generator generator = new Generator();

        [TestMethod]
        public void Abstract()
        {
            var value = (AbstractObject)new SimpleClass("2048", Guid.NewGuid());
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var token = generator.AsToken(t1);
            var reader = new PacketReader(t2);

            AssertExtension.MustFail<InvalidOperationException>(() => generator.ToValue<AbstractObject>(t1));
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
            var t1 = generator.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var token = generator.AsToken(t1);
            var reader = new PacketReader(t2);

            AssertExtension.MustFail<InvalidOperationException>(() => generator.ToValue<IObject>(t1), x => x.Message.StartsWith("Unable to get value"));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<IObject>(t2));


            Assert.AreEqual(value.Id, reader["Id"].GetValue<string>());
            Assert.AreEqual(((SimpleClass)value).Guid, reader["Guid"].GetValue<Guid>());

            Assert.AreEqual(value.Id, token["Id"].As<string>());
            AssertExtension.MustFail<KeyNotFoundException>(() => token["Guid"].As<Guid>());
        }
    }
}
