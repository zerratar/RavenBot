using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Commands;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Gift : TwitchCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            //var channel = cmd.Channel;
            //var session = game.GetSession(channel);
            //if (session != null)
            //{
            //    var connection = game.GetConnection(session);
            //    if (connection != null)
            //    {
            //        if (string.IsNullOrEmpty(cmd.Arguments) || !cmd.Arguments.Trim().Contains(" "))
            //        {
            //            twitch.Broadcast(channel, cmd.Sender.Username, Localization.GIFT_HELP, cmd.Command);
            //            return;
            //        }

            //        var player = session.Get(cmd.Sender);
            //        if (player != null)
            //            await connection.GiftItemAsync(player, cmd.Arguments);
            //    }
            //}
        }
    }
}
