﻿using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Clan : ChatCommandHandler
    {
        public override string Description => "Interact with a clan";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            //            ChatCommandInput.Create("interaction", "Clan interaction")
            //                .WithOptions(ChatCommandInput.Create("info", "joins the arena"),
            //                             ChatCommandInput.Create("leave", "leaves the arena"))
            //ChatCommandInput.Create("item", "What item you want to redeem").Required(),
            //ChatCommandInput.Create("amount", "How many of the said item you want to redeem")
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
                    await connection[cmd.CorrelationId].GetClanInfoAsync(player, argument);
                    return;

                case "stats":
                    // gets some statistics for the clan
                    // how many members, clan skill levels
                    // how many members of each type
                    await connection[cmd.CorrelationId].GetClanStatsAsync(player, argument);
                    return;

                case "leave":
                    await connection[cmd.CorrelationId].LeaveClanAsync(player, argument);
                    return;

                case "join":
                    // allow players to join clans that does not require invites.
                    await connection[cmd.CorrelationId].JoinClanAsync(player, argument);
                    return;

                case "remove":
                case "kick":
                    await connection[cmd.CorrelationId].RemoveFromClanAsync(player, targetPlayer);
                    return;

                case "invite":
                    await connection[cmd.CorrelationId].SendClanInviteAsync(player, targetPlayer);
                    return;

                case "accept":
                    await connection[cmd.CorrelationId].AcceptClanInviteAsync(player, argument);
                    return;

                case "decline":
                    await connection[cmd.CorrelationId].DeclineClanInviteAsync(player, argument);
                    return;

                case "promote":
                    await connection[cmd.CorrelationId].PromoteClanMemberAsync(player, targetPlayer, argument);
                    return;

                case "demote":
                    await connection[cmd.CorrelationId].DemoteClanMemberAsync(player, targetPlayer, argument);
                    return;
            }
        }
    }
}