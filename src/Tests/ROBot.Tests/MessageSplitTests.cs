using Microsoft.VisualStudio.TestTools.UnitTesting;
using RavenBot.Core;
using System.Linq;
using System.Text;

namespace ROBot.Tests
{
    [TestClass]
    public class MessageSplitTests
    {
        private static string Words(int count)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < count; ++i)
            {
                if (i > 0) sb.Append(' ');
                sb.Append("word").Append(i);
            }
            return sb.ToString();
        }

        [TestMethod]
        public void ShortMessageIsOnePart()
        {
            var parts = MessageUtilities.SplitMessage("hello there");
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("hello there", parts[0]);
        }

        [TestMethod]
        public void EmptyMessageHasNoParts()
        {
            Assert.AreEqual(0, MessageUtilities.SplitMessage("").Count);
            Assert.AreEqual(0, MessageUtilities.SplitMessage(null).Count);
        }

        [TestMethod]
        public void LongTwitchReplyFitsTheLimitAndKeepsEveryWord()
        {
            var message = Words(300);
            Assert.IsTrue(message.Length > 1000);

            var parts = MessageUtilities.SplitMessage(message);
            Assert.IsTrue(parts.Count > 1);
            Assert.IsTrue(parts.All(p => p.Length <= MessageUtilities.TwitchMaxMessageLength && p.Length <= 500));
            Assert.AreEqual(message, string.Join(" ", parts));
        }

        [TestMethod]
        public void LongDiscordMessageFitsTheLimit()
        {
            var message = Words(1000);
            var parts = MessageUtilities.SplitMessage(message, MessageUtilities.DiscordMaxMessageLength);
            Assert.IsTrue(parts.Count > 1);
            Assert.IsTrue(parts.All(p => p.Length <= 2000));
            Assert.AreEqual(message, string.Join(" ", parts));
        }

        [TestMethod]
        public void TextWithoutSpacesIsCutHard()
        {
            var message = new string('a', 1200);
            var parts = MessageUtilities.SplitMessage(message);
            Assert.AreEqual(3, parts.Count);
            Assert.IsTrue(parts.All(p => p.Length <= MessageUtilities.TwitchMaxMessageLength));
            Assert.AreEqual(message, string.Concat(parts));
        }
    }
}
