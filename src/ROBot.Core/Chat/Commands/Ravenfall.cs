using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Ravenfall : ChatCommandHandler
    {
        public override bool RequiresBroadcaster => false;
        public override string Category => "Game";
        public override string Description => "Forces the game to load the update scene. This is the same as restarting the game to initiate an update. Only broadcaster, game admin and moderators can use this command.";

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    if (cmd.Arguments == null || cmd.Arguments.Length == 0)
                    {
                        await chat.SendMessageAsync(cmd.Channel, "Ravenfall is a Twitch idle game where you can train, craft, fight together against huge raid bosses or fight against eachother.", new object[0]);
                    }
                    else if (cmd.Arguments.StartsWith("update", System.StringComparison.OrdinalIgnoreCase))
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
                        await chat.SendMessageAsync(cmd.Channel, "Please see https://www.ravenfall.stream/how-to-play on how to play Ravenfall. This guide is still being updated so make sure to check it out frequently.", new object[0]);
                    }
                }
            }
        }
    }

    //public class Update : ChatCommandHandler
    //{
    //    public override bool RequiresBroadcaster => true;
    //    public override string Category => "Game";
    //    public override string Description => "Forces the game to load the update scene. This is the same as restarting the game to initiate an update. Only broadcaster, game admin and moderators can use this command.";

    //    public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
    //    {
    //        var channel = cmd.Channel;
    //        var session = game.GetSession(channel);
    //        if (session != null)
    //        {
    //            var connection = game.GetConnection(session);
    //            if (connection != null)
    //            {
    //                var player = session.Get(cmd);
    //                await connection[cmd].UpdateGameAsync(player);
    //            }
    //        }
    //    }
    //}

}
