using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;

namespace Mikodev.Testing
{
    [TestClass]
    public class EmptyType
    {
        private readonly struct EmptyStructure { }

        private sealed class EmptyClass { }

        private static Cache cache = new Cache();

        private void MustFail<E>(Action action) where E : Exception
        {
            try { action.Invoke(); }
            catch (E) { return; }
            Assert.Fail();
        }

        [TestMethod]
        public void Anonymous()
        {
            var anonymous = new { };
            MustFail<InvalidOperationException>(() => cache.Serialize(anonymous));
            MustFail<InvalidOperationException>(() => cache.Deserialize(Array.Empty<byte>(), anonymous));

            MustFail<PacketException>(() => PacketConvert.Serialize(anonymous));
            MustFail<PacketException>(() => PacketConvert.Deserialize(Array.Empty<byte>(), anonymous));
        }

        [TestMethod]
        public void Structure()
        {
            var empty = default(EmptyStructure);
            MustFail<InvalidOperationException>(() => cache.Serialize(empty));
            MustFail<InvalidOperationException>(() => cache.Deserialize<EmptyStructure>(Array.Empty<byte>()));

            MustFail<PacketException>(() => PacketConvert.Serialize(empty));
            MustFail<PacketException>(() => PacketConvert.Deserialize<EmptyStructure>(Array.Empty<byte>()));
        }

        [TestMethod]
        public void Class()
        {
            var empty = new EmptyClass();
            MustFail<InvalidOperationException>(() => cache.Serialize(empty));
            MustFail<InvalidOperationException>(() => cache.Deserialize<EmptyClass>(Array.Empty<byte>()));

            MustFail<PacketException>(() => PacketConvert.Serialize(empty));
            MustFail<PacketException>(() => PacketConvert.Deserialize<EmptyClass>(Array.Empty<byte>()));
        }
    }
}
