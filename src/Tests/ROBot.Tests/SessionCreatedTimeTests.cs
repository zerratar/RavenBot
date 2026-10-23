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
        public void ParsesTheFormatsOlderGamesSendFromOtherCultures()
        {
            var now = new DateTime(2026, 10, 23, 20, 0, 0);
            var expected = new DateTime(2026, 10, 23, 19, 15, 30);
            foreach (var text in new[]
            {
                "23/10/2026 19:15:30",      // en-GB, fr-FR, es-ES
                "10/23/2026 7:15:30 PM",    // en-US
                "23.10.2026 19:15:30",      // de-DE, pl-PL, ru-RU
                "23.10.2026 19.15.30",      // fi-FI
                "23-10-2026 19:15:30",      // nl-NL
                "2026-10-23 19:15:30",      // sv-SE
                "2026/10/23 19:15:30",      // ja-JP
            })
            {
                Assert.IsTrue(SessionCreatedTime.TryParse(text, now, out var created), text);
                Assert.AreEqual(expected, created, text);
            }
        }

        [TestMethod]
        public void AnAmbiguousDayAndMonthIsTheOneClosestToNow()
        {
            // 05/10/2026 is 5 October day first, or 10 May month first; a session is recent, so 5 October
            var now = new DateTime(2026, 10, 6, 12, 0, 0);
            Assert.IsTrue(SessionCreatedTime.TryParse("05/10/2026 09:00:00", now, out var created));
            Assert.AreEqual(new DateTime(2026, 10, 5, 9, 0, 0), created);

            // and the other way round in May
            now = new DateTime(2026, 5, 11, 12, 0, 0);
            Assert.IsTrue(SessionCreatedTime.TryParse("05/10/2026 09:00:00", now, out created));
            Assert.AreEqual(new DateTime(2026, 5, 10, 9, 0, 0), created);
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
