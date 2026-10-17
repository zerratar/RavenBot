using Microsoft.VisualStudio.TestTools.UnitTesting;
using ROBot.Core.GameServer;
using System;
using System.Linq;

namespace ROBot.Tests
{
    [TestClass]
    public class PendingCommandQueueTests
    {
        private DateTime now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        private PendingCommandQueue Create(int maxPerUser = 10)
        {
            return new PendingCommandQueue(TimeSpan.FromMinutes(2), maxPerUser, () => now);
        }

        [TestMethod]
        public void CommandsComeBackInOrderOnce()
        {
            var queue = Create();
            var user = Guid.NewGuid();
            queue.Enqueue(user, "a");
            queue.Enqueue(user, "b");
            queue.Enqueue(user, "c");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, queue.TakeAll(user).ToArray());
            Assert.AreEqual(0, queue.TakeAll(user).Count);
        }

        [TestMethod]
        public void CommandsAreKeptPerUser()
        {
            var queue = Create();
            var alice = Guid.NewGuid();
            var bob = Guid.NewGuid();
            queue.Enqueue(alice, "a1");
            queue.Enqueue(bob, "b1");
            queue.Enqueue(alice, "a2");

            CollectionAssert.AreEqual(new[] { "b1" }, queue.TakeAll(bob).ToArray());
            CollectionAssert.AreEqual(new[] { "a1", "a2" }, queue.TakeAll(alice).ToArray());
        }

        [TestMethod]
        public void OldCommandsAreDropped()
        {
            var queue = Create();
            var user = Guid.NewGuid();
            queue.Enqueue(user, "ancient");
            now = now.AddMinutes(3);
            queue.Enqueue(user, "fresh");
            now = now.AddSeconds(30);

            CollectionAssert.AreEqual(new[] { "fresh" }, queue.TakeAll(user).ToArray());
        }

        [TestMethod]
        public void EverythingExpiredGivesNothing()
        {
            var queue = Create();
            var user = Guid.NewGuid();
            queue.Enqueue(user, "a");
            now = now.AddMinutes(5);

            Assert.AreEqual(0, queue.Count(user));
            Assert.AreEqual(0, queue.TakeAll(user).Count);
        }

        [TestMethod]
        public void QueueIsCappedPerUserKeepingTheNewest()
        {
            var queue = Create(maxPerUser: 3);
            var user = Guid.NewGuid();
            for (var i = 0; i < 5; ++i)
            {
                queue.Enqueue(user, "c" + i);
            }

            CollectionAssert.AreEqual(new[] { "c2", "c3", "c4" }, queue.TakeAll(user).ToArray());
        }

        [TestMethod]
        public void ClearForgetsTheUser()
        {
            var queue = Create();
            var user = Guid.NewGuid();
            queue.Enqueue(user, "a");
            queue.Clear(user);
            Assert.AreEqual(0, queue.TakeAll(user).Count);
        }

        [TestMethod]
        public void EmptyUserOrRequestIsIgnored()
        {
            var queue = Create();
            queue.Enqueue(Guid.Empty, "a");
            Assert.AreEqual(0, queue.Count(Guid.Empty));

            var user = Guid.NewGuid();
            queue.Enqueue(user, "");
            queue.Enqueue(user, null);
            Assert.AreEqual(0, queue.Count(user));
        }
    }
}
