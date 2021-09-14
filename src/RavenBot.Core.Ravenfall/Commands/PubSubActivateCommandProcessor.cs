using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PubSubActivateCommandProcessor : Net.RavenfallCommandProcessor
    {
        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (broadcaster.CanRecieveChannelPointRewards)
            {
                broadcaster.Broadcast(cmd.Sender.Username, "Channel Point Rewards is all good SeemsGood");
            }
            else
            {
                if (cmd.Sender.IsBroadcaster)
                {
                    broadcaster.Broadcast(cmd.Sender.Username, "Channel Point Rewards does not seem to be activated. Please go to https://www.ravenfall.stream/api/auth/activate-pubsub to activate it.");
                }
                else
                {
                    broadcaster.Broadcast(cmd.Sender.Username, "Channel Point Rewards does not seem to be activated.");
                }
            }
        }
    }
}