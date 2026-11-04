using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Farm : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Train Farming. Name a crop to farm only that one, or leave it out to farm anything your level allows.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("crop", "The crop to farm, such as potato or wheat; leave it out to farm anything.").NotRequired(),
        };

        public override string UsageExample => "!farm potato";

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
                    await connection[cmd].FarmAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
