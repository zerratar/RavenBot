using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class PubSub : ChatCommandHandler
    {
        public override string Category => "Misc";
        public override string Description => "Tells whether channel point rewards work in this stream. They are turned off on Twitch for now: the Twitch PubSub service they used has been shut down.";
        public override string UsageExample => "!pubsub";

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);

            if (session != null)
            {
                if (!(chat is ITwitchCommandClient twitch))
                {
                    await chat.SendReplyAsync(cmd, "Channel Point Rewards is only available on Twitch.");
                    return;
                }

                // the PubSub connection is no longer made (Twitch shut PubSub down), and this answered nothing at all
                await chat.SendReplyAsync(cmd, "Channel point rewards are turned off on Twitch for now, as the Twitch service they used has been shut down.");
                return;
            }
        }
    }
}
