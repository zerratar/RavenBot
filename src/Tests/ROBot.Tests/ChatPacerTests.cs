using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROBot.Core.Chat.Twitch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Tests
{
    [TestClass]
    public class ChatPacerTests
    {
        private DateTime now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

        private ChatPacer Create(int maxPerChannel = 60, int verified = 0)
        {
            return new ChatPacer(TimeSpan.FromSeconds(120), maxPerChannel, verified, () => now);
        }

        private static List<ChatPacer.Outgoing> TakeAll(ChatPacer pacer)
        {
            var taken = new List<ChatPacer.Outgoing>();
            while (pacer.TryTake(out var m)) taken.Add(m);
            return taken;
        }

        [TestMethod]
        public void OneMessageGoesStraightAway()
        {
            var pacer = Create();
            pacer.Enqueue("zerratar", "hello");
            var taken = TakeAll(pacer);
            Assert.AreEqual(1, taken.Count);
            Assert.AreEqual("hello", taken[0].Text);
        }

        [TestMethod]
        public void AFewMessagesKeepTheirOwnLinesOneASecond()
        {
            var pacer = Create();
            pacer.Enqueue("zerratar", "a");
            pacer.Enqueue("zerratar", "b");
            pacer.Enqueue("zerratar", "c");

            Assert.AreEqual("a", TakeAll(pacer).Single().Text);
            now = now.AddMilliseconds(500);
            Assert.AreEqual(0, TakeAll(pacer).Count, "one a second in a channel where the bot is not a moderator");
            now = now.AddMilliseconds(700);
            Assert.AreEqual("b", TakeAll(pacer).Single().Text);
            now = now.AddSeconds(1.2);
            Assert.AreEqual("c", TakeAll(pacer).Single().Text);
        }

        [TestMethod]
        public void APileOfAnnouncementsIsJoined()
        {
            var pacer = Create();
            for (var i = 0; i < 10; i++) pacer.Enqueue("zerratar", "player" + i + " you found a Golden Egg");
            var taken = TakeAll(pacer);
            Assert.AreEqual(1, taken.Count);
            StringAssert.StartsWith(taken[0].Text, "player0 you found a Golden Egg player1");
            Assert.AreEqual(0, pacer.Waiting);
        }

        [TestMethod]
        public void JoinedMessagesStayUnderTwitchsLength()
        {
            var pacer = Create();
            var line = new string('x', 120);
            for (var i = 0; i < 10; i++) pacer.Enqueue("zerratar", line);
            var first = TakeAll(pacer).Single();
            Assert.IsTrue(first.Text.Length <= ChatPacer.MaxLength);
            Assert.AreEqual(4 * 120 + 3, first.Text.Length);
        }

        [TestMethod]
        public void RepliesAreNeverJoined()
        {
            var pacer = Create();
            pacer.SetPrivileged("zerratar", true);
            for (var i = 0; i < 6; i++) pacer.Enqueue("zerratar", "reply " + i, "msg" + i);
            var taken = TakeAll(pacer);
            Assert.AreEqual(6, taken.Count);
            Assert.AreEqual("msg3", taken[3].ReplyTo);
        }

        [TestMethod]
        public void AllChannelsShareTheAccountsTwenty()
        {
            var pacer = Create();
            for (var i = 0; i < 30; i++) pacer.Enqueue("channel" + i, "hi");
            Assert.AreEqual(ChatPacer.NormalLimit, TakeAll(pacer).Count);
            now = now.AddSeconds(10);
            Assert.AreEqual(0, TakeAll(pacer).Count);
            now = now.AddSeconds(21);
            Assert.AreEqual(30 - ChatPacer.NormalLimit, TakeAll(pacer).Count, "the rest go once the window has passed, none lost");
        }

        [TestMethod]
        public void AModeratorChannelMaySendMore()
        {
            var pacer = Create();
            pacer.SetPrivileged("zerratar", true);
            for (var i = 0; i < 40; i++) pacer.Enqueue("zerratar", "reply", "id" + i);
            Assert.AreEqual(40, TakeAll(pacer).Count);
            pacer.Enqueue("someone", "hi");
            Assert.AreEqual(0, TakeAll(pacer).Count, "the moderator channel's messages count toward the 20 too");
        }

        [TestMethod]
        public void ChannelsTakeTurns()
        {
            var pacer = Create();
            pacer.SetPrivileged("a", true);
            pacer.SetPrivileged("b", true);
            for (var i = 0; i < 3; i++) pacer.Enqueue("a", "a" + i, "ra" + i);
            pacer.Enqueue("b", "b0", "rb0");
            var order = string.Join(",", TakeAll(pacer).Select(m => m.Text));
            Assert.AreEqual("a0,b0,a1,a2", order);
        }

        [TestMethod]
        public void OldMessagesGo()
        {
            var pacer = Create();
            for (var i = 0; i < 25; i++) pacer.Enqueue("c" + i, "hi");
            TakeAll(pacer);
            now = now.AddMinutes(3);
            Assert.AreEqual(0, TakeAll(pacer).Count);
            Assert.AreEqual(25 - ChatPacer.NormalLimit, pacer.Dropped);
        }

        [TestMethod]
        public void TooManyWaitingDropsTheOldest()
        {
            var pacer = Create(maxPerChannel: 3);
            pacer.SetPrivileged("zerratar", true);
            for (var i = 0; i < 5; i++) pacer.Enqueue("zerratar", "r" + i, "id" + i);
            CollectionAssert.AreEqual(new[] { "r2", "r3", "r4" }, TakeAll(pacer).Select(m => m.Text).ToArray());
            Assert.AreEqual(2, pacer.Dropped);
        }

        [TestMethod]
        public void ARateLimitNoticeSlowsTheChannel()
        {
            var pacer = Create();
            pacer.SetPrivileged("zerratar", true);
            pacer.Slow("zerratar", TimeSpan.FromSeconds(10));
            pacer.Enqueue("zerratar", "hi");
            Assert.AreEqual(0, TakeAll(pacer).Count);
            now = now.AddSeconds(11);
            Assert.AreEqual(1, TakeAll(pacer).Count);
            Assert.IsFalse(pacer.IsPrivileged("zerratar"));
        }

        [TestMethod]
        public void AVerifiedBotHasItsOwnLimit()
        {
            var pacer = Create(verified: 7500);
            for (var i = 0; i < 300; i++) pacer.Enqueue("c" + i, "hi");
            Assert.AreEqual(300, TakeAll(pacer).Count);
        }
    }
}
