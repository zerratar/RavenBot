using ROBot.Core.GameServer;
using Shinobytes.Ravenfall.RavenNet.Core;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Twitch
{
    public class TwitchChatMessageHandler : ITwitchChatMessageHandler
    {
        private readonly IMessageBus messageBus;

        public void Dispose()
        {
        }

        public TwitchChatMessageHandler(IMessageBus messageBus)
        {
            this.messageBus = messageBus;
        }

        public Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ChatMessage msg)
        {
            var channel = msg.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                //session.SendChatMessage(msg.Username, msg.Message);
                if (msg.Bits > 0)
                {
                    this.messageBus.Send(
                        nameof(ROBot.Core.Twitch.TwitchCheer),
                        new ROBot.Core.Twitch.TwitchCheer(
                            msg.Channel,
                            msg.Id,
                            msg.Username,
                            msg.DisplayName,
                            msg.IsModerator,
                            msg.IsSubscriber,
                            msg.IsVip,
                            msg.Bits)
                    );
                }

            }
            return Task.CompletedTask;
        }
    }
}