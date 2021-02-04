using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Duel : TwitchCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd.Sender);
                    if (string.IsNullOrWhiteSpace(cmd.Arguments))
                    {
                        //twitch.Broadcast(cmd.Sender.Username, "To duel a player you need to specify their name. ex: '!duel JohnDoe', to accept or decline a duel request use '!duel accept' or '!duel decline'. You may also cancel an ongoing request by using '!duel cancel'");
                        return;
                    }

                    var sub = cmd.Arguments?.Trim();
                    if (sub.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection.CancelDuelRequestAsync(player);
                    }
                    else if (sub.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection.AcceptDuelRequestAsync(player);
                    }
                    else if (sub.Equals("decline", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection.DeclineDuelRequestAsync(player);
                    }
                    else
                    {
                        await connection.DuelRequestAsync(player, session.GetUserByName(sub));
                    }
                    await connection.DungeonStartAsync(player);
                }
            }
        }
    }
}
