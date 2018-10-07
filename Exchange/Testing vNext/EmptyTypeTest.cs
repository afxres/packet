using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;

namespace Mikodev.Testing
{
    [TestClass]
    public class EmptyTypeTest
    {
        private readonly struct EmptyStructure { }

        private sealed class EmptyClass { }

        private sealed class SetOnlyClass
        {
            public int Number { set { } }
        }

        private sealed class GetOnlyClass
        {
            public int Number { get => default; }
        }

        private static Cache cache = new Cache();

        [TestMethod]
        public void Anonymous()
        {
            var anonymous = new { };
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(anonymous));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue(Array.Empty<byte>(), anonymous));

            AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(anonymous));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize(Array.Empty<byte>(), anonymous));
        }

        [TestMethod]
        public void Structure()
        {
            var empty = default(EmptyStructure);
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(empty));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue<EmptyStructure>(Array.Empty<byte>()));

            AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(empty));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<EmptyStructure>(Array.Empty<byte>()));
        }

        [TestMethod]
        public void Class()
        {
            var empty = new EmptyClass();
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(empty));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue<EmptyClass>(Array.Empty<byte>()));

            AssertExtension.MustFail<PacketException>(() => PacketConvert.Serialize(empty));
            AssertExtension.MustFail<PacketException>(() => PacketConvert.Deserialize<EmptyClass>(Array.Empty<byte>()));
        }

        [TestMethod]
        public void SetOnly()
        {
            var empty = new SetOnlyClass();
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(empty), x => x.Message.Contains("No available property found"));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue<SetOnlyClass>(Array.Empty<byte>()), x => x.Message.Contains("No available property found"));
        }

        [TestMethod]
        public void GetOnly()
        {
            var empty = new GetOnlyClass();
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToBytes(empty), x => x.Message.Contains("does not have a public setter"));
            AssertExtension.MustFail<InvalidOperationException>(() => cache.ToValue<GetOnlyClass>(Array.Empty<byte>()), x => x.Message.Contains("does not have a public setter"));
        }
    }
}
