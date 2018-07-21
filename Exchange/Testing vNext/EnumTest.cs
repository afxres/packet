using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Testing
{
    [TestClass]
    public class EnumTest
    {
        private enum SimpleEnum : long
        {
            Zero = 0,
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight,
            Nine,
            Ten,
        }

        private const int loop = 32;
        private readonly Cache cache = new Cache();
        private readonly Random random = new Random();

        [TestMethod]
        public void Value()
        {
            for (int i = 0; i < loop; i++)
            {
                var anonymous = new
                {
                    day = (DayOfWeek)random.Next(0, 7),
                    number = (SimpleEnum)random.Next(0, 11),
                };
                var t1 = cache.Serialize(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = PacketConvert.Deserialize(t1, anonymous);
                var r2 = cache.Deserialize(t2, anonymous);

                Assert.AreEqual(anonymous, r1);
                Assert.AreEqual(anonymous, r2);
            }
        }

        [TestMethod]
        public void Collection()
        {
            for (int i = 0; i < loop; i++)
            {
                void AssertLegacy<T, U>(T days, U numbers) where T : IEnumerable<DayOfWeek> where U : IEnumerable<SimpleEnum>
                {
                    var anonymous = new
                    {
                        day = (DayOfWeek)random.Next(0, 7),
                        number = (SimpleEnum)random.Next(0, 11),
                        days,
                        numbers,
                    };
                    var t1 = cache.Serialize(anonymous);
                    var t2 = PacketConvert.Serialize(anonymous);
                    var r1 = PacketConvert.Deserialize(t1, anonymous);
                    var r2 = cache.Deserialize(t2, anonymous);

                    Assert.AreEqual(anonymous.day, r1.day);
                    Assert.AreEqual(anonymous.number, r1.number);
                    Assert.IsTrue(anonymous.days.SequenceEqual(r1.days));
                    Assert.IsTrue(anonymous.numbers.SequenceEqual(r1.numbers));

                    Assert.AreEqual(anonymous.day, r2.day);
                    Assert.AreEqual(anonymous.number, r2.number);
                    Assert.IsTrue(anonymous.days.SequenceEqual(r2.days));
                    Assert.IsTrue(anonymous.numbers.SequenceEqual(r2.numbers));
                }

                var daySeq = Enumerable.Range(0, 32).Select(x => (DayOfWeek)random.Next(0, 7));
                var numberSeq = Enumerable.Range(0, 32).Select(x => (SimpleEnum)random.Next(0, 11));
                AssertLegacy(daySeq.ToArray(), numberSeq.ToArray());
                AssertLegacy(daySeq.ToList(), (IList<SimpleEnum>)numberSeq.ToList());
                AssertLegacy((IList<DayOfWeek>)daySeq.ToList(), numberSeq.ToList());
                AssertLegacy(new HashSet<DayOfWeek>(daySeq), (ISet<SimpleEnum>)new HashSet<SimpleEnum>(numberSeq));
                AssertLegacy((ISet<DayOfWeek>)new HashSet<DayOfWeek>(daySeq), new HashSet<SimpleEnum>(numberSeq));
            }
        }
    }
}
