using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Stats : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Command that allows for toggle helmet visibility or cycle active pet.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "What do you want to toggle?", "Helmet", "Pet").Required()
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd);
                    await connection[cmd].RequestPlayerStatsAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
