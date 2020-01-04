using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System.Linq;

namespace Mikodev.Tests
{
    [TestClass]
    public class DynamicWriterTests
    {
        [TestMethod]
        public void DynamicObjectArray()
        {
            var target = Enumerable.Range(0, 16).Select(x => (dynamic)new PacketWriter()).ToArray();
            for (var i = 0; i < target.Length; i++)
            {
                var writer = target[i];
                writer.id = i;
                writer.body.name = $"{i:d2}";
            }
            var buffer = PacketConvert.Serialize(target.Cast<PacketWriter>());
            var result = PacketConvert.Deserialize(buffer, anonymous: new[] { new { id = 0, body = new { name = (string)null } } });
            Assert.AreEqual(16, result.Length);
            for (var i = 0; i < result.Length; i++)
            {
                var item = result[i];
                Assert.IsNotNull(item);
                Assert.AreEqual(i, item.id);
                Assert.AreEqual($"{i:d2}", item.body.name);
            }
        }
    }
}
