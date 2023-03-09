using RavenBot.Core.Chat;
using ROBot.Core.GameServer;
using Shinobytes.Core;
using System.Threading.Tasks;
using TwitchLib.Client.Models;

namespace ROBot.Core.Chat.Twitch
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

        public Task HandleAsync(
            IBotServer game,
            ITwitchCommandClient twitch,
            TwitchLib.Client.Models.ChatMessage msg)
        {
            var channel = msg.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                //session.SendChatMessage(msg.Username, msg.Message);
                if (msg.Bits > 0)
                {
                    messageBus.Send(
                        nameof(CheerBitsEvent),
                        new CheerBitsEvent(
                            "twitch",
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