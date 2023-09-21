using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PubSubActivateCommandProcessor : Net.RavenfallCommandProcessor
    {
        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (chat.CanRecieveChannelPointRewards)
            {
                await chat.SendReplyAsync(cmd, "Channel Point Rewards is all good SeemsGood");
            }
            else
            {
                if (cmd.Sender.IsBroadcaster)
                {
                    await chat.SendReplyAsync(cmd, "Channel Point Rewards does not seem to be activated. Please go to https://www.ravenfall.stream/api/auth/activate-pubsub to activate it.");
                }
                else
                {
                    await chat.SendReplyAsync(cmd, "Channel Point Rewards does not seem to be activated.");
                }
            }
        }
    }
}