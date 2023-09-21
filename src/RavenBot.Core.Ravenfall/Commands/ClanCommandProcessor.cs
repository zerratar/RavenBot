using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ClanCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public ClanCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.Arguments))
                return;

            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);

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
                    await this.game[cmd.CorrelationId].GetClanInfoAsync(player, argument);
                    return;

                case "stats":
                    // gets some statistics for the clan
                    // how many members, clan skill levels
                    // how many members of each type
                    await this.game[cmd.CorrelationId].GetClanStatsAsync(player, argument);
                    return;

                case "leave":
                    await this.game[cmd.CorrelationId].LeaveClanAsync(player, argument);
                    return;

                case "join":
                    // allow players to join clans that does not require invites.
                    await this.game[cmd.CorrelationId].JoinClanAsync(player, argument);
                    return;

                case "remove":
                case "kick":
                    await this.game[cmd.CorrelationId].RemoveFromClanAsync(player, targetPlayer);
                    return;

                case "invite":
                    await this.game[cmd.CorrelationId].SendClanInviteAsync(player, targetPlayer);
                    return;

                case "accept":
                    await this.game[cmd.CorrelationId].AcceptClanInviteAsync(player, argument);
                    return;

                case "decline":
                    await this.game[cmd.CorrelationId].DeclineClanInviteAsync(player, argument);
                    return;

                case "promote":
                    await this.game[cmd.CorrelationId].PromoteClanMemberAsync(player, targetPlayer, argument);
                    return;

                case "demote":
                    await this.game[cmd.CorrelationId].DemoteClanMemberAsync(player, targetPlayer, argument);
                    return;
            }
        }
    }
}