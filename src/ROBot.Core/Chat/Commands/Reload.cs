using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Reload : ChatCommandHandler
    {
        public override bool RequiresBroadcaster => true;
        public override string Category => "Game";
        public override string Description => "Forces the game to reload the active scene. This is the same as pressing F5 in game. Only broadcaster, game admin and moderators can use this command.";
        
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
                    await connection[cmd].ReloadGameAsync(player);
                }
            }
        }
    }
}
