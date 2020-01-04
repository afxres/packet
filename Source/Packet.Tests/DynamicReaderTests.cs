using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System.Linq;

namespace Mikodev.Tests
{
    [TestClass]
    public class DynamicReaderTests
    {
        [TestMethod]
        public void DynamicObjectArray()
        {
            var source = Enumerable.Range(0, 16).Select(x => new { id = x, name = $"{x:x2}" }).ToArray();
            var buffer = PacketConvert.Serialize(source);
            var result = PacketConvert.Deserialize<byte[][]>(buffer).Select(x => (dynamic)new PacketReader(x)).ToArray();
            for (var i = 0; i < result.Length; i++)
            {
                var item = result[i];
                var id = (int)item.id;
                var name = (string)item.name;
                Assert.AreEqual(source[i].id, id);
                Assert.AreEqual(source[i].name, name);
            }
        }
    }
}
