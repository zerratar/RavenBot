using System;
using System.Collections.Generic;

namespace ROBot.Core.GameServer
{
    /// <summary>
    /// Commands waiting for a game session whose link is down, kept per Ravenfall user id so they
    /// survive the connection that queued them and are sent on whichever connection takes the session
    /// next. Old commands are dropped rather than replayed long after they were typed.
    /// </summary>
    public class PendingCommandQueue
    {
        public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);
        public const int DefaultMaxPerUser = 200;

        private readonly object mutex = new object();
        private readonly Dictionary<Guid, Queue<Entry>> queues = new Dictionary<Guid, Queue<Entry>>();
        private readonly TimeSpan maxAge;
        private readonly int maxPerUser;
        private readonly Func<DateTime> utcNow;

        public PendingCommandQueue()
            : this(DefaultMaxAge, DefaultMaxPerUser, null)
        {
        }

        public PendingCommandQueue(TimeSpan maxAge, int maxPerUser, Func<DateTime> utcNow)
        {
            this.maxAge = maxAge;
            this.maxPerUser = Math.Max(1, maxPerUser);
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public void Enqueue(Guid userId, string request)
        {
            if (userId == Guid.Empty || string.IsNullOrEmpty(request))
            {
                return;
            }

            var now = utcNow();
            lock (mutex)
            {
                if (!queues.TryGetValue(userId, out var queue))
                {
                    queues[userId] = queue = new Queue<Entry>();
                }

                DropExpired(queue, now);

                while (queue.Count >= maxPerUser)
                {
                    queue.Dequeue();
                }

                queue.Enqueue(new Entry(now, request));
            }
        }

        /// <summary>
        /// Removes and returns every command still young enough to send, oldest first.
        /// </summary>
        public IReadOnlyList<string> TakeAll(Guid userId)
        {
            var result = new List<string>();
            var now = utcNow();
            lock (mutex)
            {
                if (!queues.TryGetValue(userId, out var queue))
                {
                    return result;
                }

                queues.Remove(userId);
                DropExpired(queue, now);
                foreach (var entry in queue)
                {
                    result.Add(entry.Request);
                }
            }
            return result;
        }

        public void Clear(Guid userId)
        {
            lock (mutex)
            {
                queues.Remove(userId);
            }
        }

        public int Count(Guid userId)
        {
            var now = utcNow();
            lock (mutex)
            {
                if (!queues.TryGetValue(userId, out var queue))
                {
                    return 0;
                }

                DropExpired(queue, now);
                return queue.Count;
            }
        }

        private void DropExpired(Queue<Entry> queue, DateTime now)
        {
            while (queue.Count > 0 && now - queue.Peek().QueuedUtc > maxAge)
            {
                queue.Dequeue();
            }
        }

        private readonly struct Entry
        {
            public readonly DateTime QueuedUtc;
            public readonly string Request;

            public Entry(DateTime queuedUtc, string request)
            {
                QueuedUtc = queuedUtc;
                Request = request;
            }
        }
    }
}
