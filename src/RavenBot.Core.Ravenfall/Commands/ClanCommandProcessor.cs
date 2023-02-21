using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ClanCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ClanCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.Arguments))
                return;

            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);

            var values = cmd.Arguments.Split(' ');
            if (values.Length <= 0)
                return;


            values = values.Select(x => x.Trim()).ToArray();

            var action = values[0].ToLower();
            var targetPlayer = values.Length > 1 ? playerProvider.Get(values[1]) : null;
            var argument = values.Length > 1 && targetPlayer == null ? values[1] : values.Length > 2 ? values[2] : null;
            if (string.IsNullOrEmpty(argument)) argument = "-";
            switch (action)
            {
                case "info":
                    // clan info, displays the current clan and clan level
                    await game.GetClanInfoAsync(player, argument);
                    return;

                case "stats":
                    // gets some statistics for the clan
                    // how many members, clan skill levels
                    // how many members of each type
                    await game.GetClanStatsAsync(player, argument);
                    return;

                case "leave":
                    await game.LeaveClanAsync(player, argument);
                    return;

                case "join":
                    // allow players to join clans that does not require invites.
                    await game.JoinClanAsync(player, argument);
                    return;

                case "remove":
                case "kick":
                    await game.RemoveFromClanAsync(player, targetPlayer);
                    return;

                case "invite":
                    await game.SendClanInviteAsync(player, targetPlayer);
                    return;

                case "accept":
                    await game.AcceptClanInviteAsync(player, argument);
                    return;

                case "decline":
                    await game.DeclineClanInviteAsync(player, argument);
                    return;

                case "promote":
                    await game.PromoteClanMemberAsync(player, targetPlayer, argument);
                    return;

                case "demote":
                    await game.DemoteClanMemberAsync(player, targetPlayer, argument);
                    return;
            }
        }
    }
}