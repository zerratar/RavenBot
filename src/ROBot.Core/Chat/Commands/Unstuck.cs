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
        public override string Description => "Unstuck yourself in case your character got stuck, teleporting you back to the spawn point of the island you're on. Once every 30 seconds per character.";
        public override string UsageExample => "!unstuck";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "For the streamer and moderators only: a player's name, all to unstuck everyone, or training to set every idle player to train all. Leave empty to unstuck yourself.", "all", "training").NotRequired()
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
