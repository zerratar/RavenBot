using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Status : ChatCommandHandler
    {
        public override string Description => "This command allows you to get all status effects applied to your character";
        public override string UsageExample => "!status";
        public override string Category => "Character";

        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            //ChatCommandInput.Create("item", "Which item you want to examine").Required()
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
                    await connection[cmd].GetStatusEffectsAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
