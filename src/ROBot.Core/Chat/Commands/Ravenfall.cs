using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Ravenfall : ChatCommandHandler
    {
        public override bool RequiresBroadcaster => false;
        public override string Category => "Game";
        public override string Description => "Can be used to reload the game, force an update or retrieving the how-to-play guide link.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Whether you want some help, update or reload the game.", "update", "reload", "help").Required(),
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        return;
                    }

                    //if (cmd.Arguments == null || cmd.Arguments.Length == 0)
                    //{
                    //    await chat.SendMessageAsync(cmd.Channel, "Ravenfall is a Twitch idle game where you can train, craft, fight together against huge raid bosses or fight against each other.", new object[0]);
                    //}
                    //else 
                    if (cmd.Arguments.StartsWith("update", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var player = session.Get(cmd);
                        await connection[cmd].UpdateGameAsync(player);
                    }
                    else if (cmd.Arguments.StartsWith("reload", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var player = session.Get(cmd);
                        await connection[cmd].ReloadGameAsync(player);
                    }
                    else if (cmd.Arguments.Contains("help", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await chat.SendMessageAsync(cmd.Channel, "Please see https://ravenfall.fandom.com/wiki/Ravenfall on how to play Ravenfall. This guide is still being updated so make sure to check it out frequently.", new object[0]);
                    }
                }
            }
        }
    }
}
