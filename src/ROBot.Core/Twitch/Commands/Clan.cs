using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Twitch.Commands
{
    public class Clan : TwitchCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, ITwitchCommandClient twitch, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session == null)
            {
                return;
            }

            var connection = game.GetConnection(session);
            if (connection == null)
            {
                return;
            }

            var player = session.Get(cmd.Sender);
            if (player == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                return;
            }

            var values = cmd.Arguments.Split(' ');
            if (values.Length <= 0)
                return;

            values = values.Select(x => x.Trim()).ToArray();

            var action = values[0].ToLower();
            var targetPlayer = values.Length > 1 ? session.GetUserByName(values[1]) : null;
            var argument = values.Length > 1 && targetPlayer == null ? values[1] : values.Length > 2 ? values[2] : null;
            switch (action)
            {
                case "info":
                    // clan info, displays the current clan and clan level
                    await connection.GetClanInfoAsync(player, argument);
                    return;

                case "stats":
                    // gets some statistics for the clan
                    // how many members, clan skill levels
                    // how many members of each type
                    await connection.GetClanStatsAsync(player, argument);
                    return;

                case "leave":
                    await connection.LeaveClanAsync(player, argument);
                    return;

                case "join":
                    // allow players to join clans that does not require invites.
                    await connection.JoinClanAsync(player, argument);
                    return;

                case "remove":
                case "kick":
                    await connection.RemoveFromClanAsync(player, targetPlayer, argument);
                    return;

                case "invite":
                    await connection.SendClanInviteAsync(player, targetPlayer, argument);
                    return;

                case "accept":
                    await connection.AcceptClanInviteAsync(player, argument);
                    return;

                case "decline":
                    await connection.DeclineClanInviteAsync(player, argument);
                    return;

                case "promote":
                    await connection.PromoteClanMemberAsync(player, targetPlayer, argument);
                    return;

                case "demote":
                    await connection.DemoteClanMemberAsync(player, targetPlayer, argument);
                    return;
            }
        }
    }
}
