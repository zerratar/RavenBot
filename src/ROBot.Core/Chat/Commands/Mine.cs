using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Mine : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Train Mining. Name an ore to mine only that one, or leave it out to mine anything your level allows.";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("ore", "The ore to mine, such as iron ore; leave it out to mine anything.").NotRequired(),
        };

        public override string UsageExample => "!mine iron ore";

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
                    await connection[cmd].MineAsync(player, cmd.Arguments?.Trim());
                }
            }
        }
    }
}
