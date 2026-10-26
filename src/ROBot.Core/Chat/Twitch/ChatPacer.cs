using System;
using System.Collections.Generic;
using System.Linq;

namespace ROBot.Core.Chat.Twitch
{
    /// <summary>
    /// Sends chat as fast as Twitch allows, and no faster. Twitch's limits are per account, over all channels together:
    /// 20 messages per 30 seconds, and one a second in a channel, where the bot is not the broadcaster, a moderator or a
    /// VIP; 100 per 30 seconds where it is (those count toward the 20 as well). TwitchLib's own throttle counted every
    /// channel together at 20 and dropped whatever did not fit, without a word, so busy streams lost item drop messages
    /// all the time. This keeps messages waiting per channel instead, joins announcements into one message when they pile
    /// up, takes the channels in turn, and lets old messages go only when they are no use any more.
    /// </summary>
    public sealed class ChatPacer
    {
        public sealed class Outgoing
        {
            public string Channel;
            public string Text;
            /// <summary>The message a reply is threaded to; replies are never joined.</summary>
            public string ReplyTo;
            public DateTime Queued;
        }

        /// <summary>Twitch's window, and the limits kept a little under Twitch's own so a late count never costs the hour long ignore.</summary>
        public static readonly TimeSpan Window = TimeSpan.FromSeconds(30);
        public const int NormalLimit = 18;
        public const int PrivilegedLimit = 95;
        public static readonly TimeSpan NormalChannelGap = TimeSpan.FromMilliseconds(1100);
        public const int MaxLength = 500;
        /// <summary>
        /// Waiting announcements are joined only when they pile up (this many waiting, or the first waiting this long), so a
        /// streamer who picked one message per player or item gets that whenever Twitch's limits allow it.
        /// </summary>
        public const int JoinBacklog = 5;
        public static readonly TimeSpan JoinAfter = TimeSpan.FromSeconds(5);

        private readonly object sync = new object();
        private readonly Dictionary<string, LinkedList<Outgoing>> queues = new Dictionary<string, LinkedList<Outgoing>>();
        private readonly List<string> turns = new List<string>();
        private readonly Queue<DateTime> sent = new Queue<DateTime>();
        private readonly Dictionary<string, DateTime> lastInChannel = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> pausedUntil = new Dictionary<string, DateTime>();
        private readonly HashSet<string> privileged = new HashSet<string>();
        private readonly Func<DateTime> clock;
        private readonly TimeSpan maxAge;
        private readonly int maxPerChannel;
        private readonly int verifiedLimit;
        private int turn;

        /// <summary>Messages let go because they waited too long or too many waited; for the logs.</summary>
        public int Dropped { get; private set; }

        /// <param name="maxAge">How long a message may wait; an item drop a few minutes late only confuses chat.</param>
        /// <param name="maxPerChannel">Messages kept waiting per channel; beyond that the oldest go.</param>
        /// <param name="verifiedLimit">A verified bot's own limit per 30 seconds (7500), in place of the others; 0 when it is not.</param>
        public ChatPacer(TimeSpan? maxAge = null, int maxPerChannel = 60, int verifiedLimit = 0, Func<DateTime> clock = null)
        {
            this.maxAge = maxAge ?? TimeSpan.FromSeconds(120);
            this.maxPerChannel = maxPerChannel;
            this.verifiedLimit = verifiedLimit;
            this.clock = clock ?? (() => DateTime.UtcNow);
        }

        public int Waiting
        {
            get { lock (sync) return queues.Values.Sum(q => q.Count); }
        }

        public void Enqueue(string channel, string text, string replyTo = null)
        {
            if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(text)) return;
            channel = channel.ToLowerInvariant();
            lock (sync)
            {
                if (!queues.TryGetValue(channel, out var queue))
                {
                    queues[channel] = queue = new LinkedList<Outgoing>();
                    turns.Add(channel);
                }
                queue.AddLast(new Outgoing { Channel = channel, Text = text.Trim(), ReplyTo = replyTo, Queued = clock() });
                while (queue.Count > maxPerChannel)
                {
                    queue.RemoveFirst();
                    Dropped++;
                }
            }
        }

        /// <summary>From the USERSTATE Twitch sends on joining and after each message: moderator, VIP or broadcaster there.</summary>
        public void SetPrivileged(string channel, bool value)
        {
            if (string.IsNullOrWhiteSpace(channel)) return;
            channel = channel.ToLowerInvariant();
            lock (sync)
            {
                if (value) privileged.Add(channel);
                else privileged.Remove(channel);
            }
        }

        public bool IsPrivileged(string channel)
        {
            lock (sync) return channel != null && privileged.Contains(channel.ToLowerInvariant());
        }

        /// <summary>Twitch said too fast (a rate limit notice): that channel waits a while, and counts as unprivileged until Twitch says otherwise.</summary>
        public void Slow(string channel, TimeSpan pause)
        {
            if (string.IsNullOrWhiteSpace(channel)) return;
            channel = channel.ToLowerInvariant();
            lock (sync)
            {
                privileged.Remove(channel);
                pausedUntil[channel] = clock() + pause;
            }
        }

        /// <summary>
        /// The next message that may go now, if any: the channels in turn, the first one whose message the budget allows.
        /// Announcements waiting behind it in the same channel are joined to it while they fit in one message. Counted as
        /// sent when taken.
        /// </summary>
        public bool TryTake(out Outgoing message)
        {
            message = null;
            lock (sync)
            {
                var now = clock();
                while (sent.Count > 0 && now - sent.Peek() >= Window) sent.Dequeue();

                for (var i = 0; i < turns.Count; i++)
                {
                    var index = (turn + i) % turns.Count;
                    var channel = turns[index];
                    var queue = queues[channel];
                    while (queue.Count > 0 && now - queue.First.Value.Queued > maxAge)
                    {
                        queue.RemoveFirst();
                        Dropped++;
                    }
                    if (queue.Count == 0 || !MayGo(channel, now)) continue;

                    message = queue.First.Value;
                    var backlog = queue.Count >= JoinBacklog || now - message.Queued >= JoinAfter;
                    queue.RemoveFirst();
                    if (message.ReplyTo == null && backlog)
                    {
                        var joined = message.Text;
                        while (queue.Count > 0 && queue.First.Value.ReplyTo == null && joined.Length + 1 + queue.First.Value.Text.Length <= MaxLength)
                        {
                            joined += " " + queue.First.Value.Text;
                            queue.RemoveFirst();
                        }
                        message = new Outgoing { Channel = channel, Text = joined, Queued = message.Queued };
                    }

                    sent.Enqueue(now);
                    lastInChannel[channel] = now;
                    turn = (index + 1) % turns.Count;
                    return true;
                }
                return false;
            }
        }

        private bool MayGo(string channel, DateTime now)
        {
            if (pausedUntil.TryGetValue(channel, out var until) && now < until) return false;
            if (verifiedLimit > 0) return sent.Count < verifiedLimit;
            if (privileged.Contains(channel)) return sent.Count < PrivilegedLimit;
            if (lastInChannel.TryGetValue(channel, out var last) && now - last < NormalChannelGap) return false;
            return sent.Count < NormalLimit;
        }
    }
}
