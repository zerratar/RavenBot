using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Race : ChatCommandHandler
    {
        public override string Category => "Tavern";
        public override string Description => "Race your equipped pet in the tavern while the streamer is there. Use !race to join and !race reset to set up a new race once one is over.";
        public override string UsageExample => "!race";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave empty to join the race with your equipped pet, or reset to start over after a race has finished.", "reset").NotRequired(),
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection[cmd].PlayPetRacingAsync(player);
                        return;
                    }

                    if (cmd.Arguments.Contains("reset", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].ResetPetRacingAsync(player);
                        return;
                    }
                }
            }
        }
    }
}
