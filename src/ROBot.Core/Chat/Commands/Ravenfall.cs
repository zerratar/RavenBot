using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Ravenfall : ChatCommandHandler
    {
        public override bool RequiresBroadcaster => false;
        public override string Category => "Game";
        public override string Description => "Get the link to the how-to-play guide with !ravenfall help. The streamer and moderators can also reload the game or update it to the newest version.";
        public override string UsageExample => "!ravenfall help";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "help posts the how-to-play guide link. reload reloads the game and update installs a new version if there is one, both for the streamer and moderators only.", "help", "reload", "update").Required(),
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
