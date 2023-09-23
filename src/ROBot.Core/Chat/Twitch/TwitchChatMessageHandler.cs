using RavenBot.Core.Chat;
using RavenBot.Core.Chat.Twitch;
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

        public async Task HandleAsync(
            IBotServer game,
            ITwitchCommandClient twitch,
            TwitchLib.Client.Models.ChatMessage msg)
        {
            var channel = new TwitchCommand.TwitchChannel(msg.Channel);
            var session = game.GetSession(channel);
            if (session != null)
            {
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
                else
                {
                    // check if this contains the name of the bot
                    var mention = "@" + twitch.GetBotName();
                    var m = msg.Message;
                    if (string.IsNullOrEmpty(m))
                    {
                        return;
                    }

                    if (msg.Message.ToLower().Contains(mention.ToLower()))
                    {
                        // check if message contains any known command names
                        // if so, use chatgpt with description of all commands on how they can be used.
                        // ooor. just return the description of the command.
                    }
                }
            }
        }
    }
}