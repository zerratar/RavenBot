using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Clan : ChatCommandHandler
    {
        public override string Description => "Your clan: see its info, stats and your rank, join or leave one, and answer invites. Members with the right role can also invite, kick, promote and demote players.";
        public override string UsageExample => "!clan info, !clan join, !clan invite zerratar, !clan promote zerratar";
        public override string Category => "Game";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "info shows your clan, its owner, roles and members; stats its skill levels; rank (or role) your role in it. join joins a clan that needs no invite, leave leaves yours, accept or decline answers an invite. invite, kick (or remove), promote and demote act on another player, one role step at a time.", "info", "stats", "rank", "join", "leave", "accept", "decline", "invite", "kick", "promote", "demote").Required(),
            ChatCommandInput.Create("target", "For invite, kick, promote and demote: the player. For join: the clan's name or a player in it; leave it out to join the streamer's clan.").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
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

            var player = session.Get(cmd);
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
            if (string.IsNullOrEmpty(argument)) argument = "-";
            switch (action)
            {
                case "info":
                    // clan info, displays the current clan and clan level
                    await connection[cmd].GetClanInfoAsync(player, argument);
                    return;

                case "stats":
                    // gets some statistics for the clan
                    // how many members, clan skill levels
                    // how many members of each type
                    await connection[cmd].GetClanStatsAsync(player, argument);
                    return;

                case "rank":
                case "role":
                    // gets the clan rank of the current user.
                    await connection[cmd].GetClanRankAsync(player, argument);
                    return;

                case "leave":
                    await connection[cmd].LeaveClanAsync(player, argument);
                    return;

                case "join":
                    // allow players to join clans that does not require invites.
                    await connection[cmd].JoinClanAsync(player, argument);
                    return;

                case "remove":
                case "kick":
                    await connection[cmd].RemoveFromClanAsync(player, targetPlayer);
                    return;

                case "invite":
                    await connection[cmd].SendClanInviteAsync(player, targetPlayer);
                    return;

                case "accept":
                    await connection[cmd].AcceptClanInviteAsync(player, argument);
                    return;

                case "decline":
                    await connection[cmd].DeclineClanInviteAsync(player, argument);
                    return;

                case "promote":
                    await connection[cmd].PromoteClanMemberAsync(player, targetPlayer, argument);
                    return;

                case "demote":
                    await connection[cmd].DemoteClanMemberAsync(player, targetPlayer, argument);
                    return;
            }
        }
    }
}
