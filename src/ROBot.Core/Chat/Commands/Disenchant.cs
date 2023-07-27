using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Disenchant : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "This command allows you disenchant an already enchanted item. You have to be part of a clan to use this command.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "target item you want to disenchant").Required(),
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
                    if (player != null)
                        await connection[cmd].DisenchantAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
