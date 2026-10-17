using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ROBot.Core.GameServer;
using System;
using System.Globalization;
using System.Threading;

namespace ROBot.Tests
{
    [TestClass]
    public class SessionCreatedTimeTests
    {
        private static readonly DateTime Sample = new DateTime(2026, 10, 17, 21, 5, 9, DateTimeKind.Utc);

        [TestMethod]
        public void ParsesIso8601RoundTrip()
        {
            Assert.IsTrue(SessionCreatedTime.TryParse(Sample.ToString("o"), out var created));
            Assert.AreEqual(Sample, created);
        }

        [TestMethod]
        public void ParsesInvariantCultureString()
        {
            Assert.IsTrue(SessionCreatedTime.TryParse(Sample.ToString(CultureInfo.InvariantCulture), out var created));
            Assert.AreEqual(Sample.Ticks, created.Ticks);
        }

        [TestMethod]
        public void ParsesAnotherCultureWhenItIsTheCurrentCulture()
        {
            var old = Thread.CurrentThread.CurrentCulture;
            try
            {
                var swedish = CultureInfo.GetCultureInfo("sv-SE");
                var text = Sample.ToString(swedish);
                Thread.CurrentThread.CurrentCulture = swedish;
                Assert.IsTrue(SessionCreatedTime.TryParse(text, out var created));
                Assert.AreEqual(Sample.Ticks, created.Ticks);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [TestMethod]
        public void AcceptsDateAlreadyParsedByJson()
        {
            // Newtonsoft turns an ISO string inside object[] into a DateTime.
            var args = JsonConvert.DeserializeObject<object[]>("[\"" + Sample.ToString("o") + "\"]");
            Assert.IsTrue(SessionCreatedTime.TryParse(args[0], out var created));
            Assert.AreEqual(Sample, created.ToUniversalTime());

            Assert.IsTrue(SessionCreatedTime.TryParse(new JValue(Sample), out created));
            Assert.AreEqual(Sample, created);
        }

        [TestMethod]
        public void GarbageIsUnknown()
        {
            Assert.IsFalse(SessionCreatedTime.TryParse("not a date", out var created));
            Assert.IsFalse(SessionCreatedTime.IsKnown(created));
            Assert.IsFalse(SessionCreatedTime.TryParse((object)null, out created));
            Assert.IsFalse(SessionCreatedTime.TryParse("", out created));
        }

        [TestMethod]
        public void UnknownTimesNeverRefuseANewConnection()
        {
            Assert.IsFalse(SessionCreatedTime.IsOlder(SessionCreatedTime.Unknown, Sample));
            Assert.IsFalse(SessionCreatedTime.IsOlder(Sample, SessionCreatedTime.Unknown));
            Assert.IsTrue(SessionCreatedTime.IsOlder(Sample.AddMinutes(-1), Sample));
            Assert.IsFalse(SessionCreatedTime.IsOlder(Sample.AddMinutes(1), Sample));
            Assert.IsFalse(SessionCreatedTime.IsOlder(Sample, Sample));
        }
    }
}
