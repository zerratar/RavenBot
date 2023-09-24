using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Unstuck : ChatCommandHandler
    {
        public Unstuck()
        {
            RequiresBroadcaster = true;
        }
        public override string Description => "Unstuck yourself in case your character got stuck, teleporting you back to the spawn point of the island you're on.";
        public override string UsageExample => "!unstuck";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "If you're a broadcaster or moderator you can unstuck a target player.")
        };

        public override string Category => "Game";

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
                    if (!string.IsNullOrEmpty(cmd.Arguments) && (cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameAdmin || cmd.Sender.IsGameModerator))
                    {
                        if (cmd.Arguments.Trim().Equals("all", System.StringComparison.OrdinalIgnoreCase) || 
                            cmd.Arguments.Trim().Equals("everyone", System.StringComparison.OrdinalIgnoreCase) ||
                            cmd.Arguments.Trim().Equals("training", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd.CorrelationId].UnstuckAsync(player, cmd.Arguments);
                            return;
                        }

                        player = session.GetUserByName(cmd.Arguments);
                        if (player != null)
                            await connection[cmd].UnstuckAsync(player, cmd.Arguments);
                    }
                    else
                    {
                        await connection[cmd].UnstuckAsync(player, cmd.Arguments);
                    }
                }
            }
        }
    }
}
