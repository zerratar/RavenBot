using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class PubSub : ChatCommandHandler
    {
        public override string Category => "Misc";
        public override string Description => "Check pubsub connection status or get a link for activation. This is needed for getting channel point rewards to work.";
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

                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    //var state = twitch.GetPubSubState(channel);

                    //if (state == Twitch.PubSub.PubSubState.Ready)
                    //{
                    //    await chat.SendReplyAsync(cmd, "Channel Point Rewards is all good SeemsGood");
                    //    return;
                    //}
                    //else
                    //{
                    //    var player = session.Get(cmd);
                    //    if (!player.IsBroadcaster)
                    //    {
                    //        await chat.SendReplyAsync(cmd, "Channel Point Rewards currently disabled.");
                    //        return;
                    //    }

                    //    switch (state)
                    //    {
                    //        case Twitch.PubSub.PubSubState.Connecting:
                    //        case Twitch.PubSub.PubSubState.Authenticating:
                    //            await chat.SendReplyAsync(cmd, "We are currently attempting to establish a connection to Twitch PubSub...");
                    //            break;

                    //        case Twitch.PubSub.PubSubState.Disconnected:
                    //            await chat.SendReplyAsync(cmd, "Channel Reward Points have not been activated. Please sign in on " + twitch.GetPubSubActivationLink() + " to activate.");
                    //            break;

                    //        case Twitch.PubSub.PubSubState.Disposed:
                    //            await chat.SendReplyAsync(cmd, "We were unable to connect to Twitch PubSub. Please sign in again on " + twitch.GetPubSubActivationLink());
                    //            break;
                    //    }
                    //}
                }
            }
        }
    }
}
