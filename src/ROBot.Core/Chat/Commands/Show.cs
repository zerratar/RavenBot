using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{

    public class Show : ChatCommandHandler
    {
        public Show()
        {
            RequiresBroadcaster = true;
        }
        public override string Category => "Game";
        public override string Description => "Points the game camera at your character. VIPs can also show an island, and the streamer and moderators any player or island.";
        public override string UsageExample => "!show";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target",
                "Leave empty to show yourself. An island name (home, away, ironhill, kyo, heim, atria, eldara) for VIPs, moderators and the streamer. " +
                "A player's name, or islands or players to cycle to the next one, for moderators and the streamer.").NotRequired()
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
                    var targetPlayerName = cmd.Arguments?.Trim();
                    var targetPlayer = session.Get(cmd.Sender);

                    if (!string.IsNullOrEmpty(targetPlayerName))
                    {
                        if ((!IsIsland(targetPlayerName) || !cmd.IsVipPlus()) && !cmd.IsModeratorPlus())
                        {
                            await chat.SendReplyAsync(cmd, Localization.OBSERVE_PERM);
                            return;
                        }
                        else
                        {
                            targetPlayer = session.GetUserByName(targetPlayerName);
                        }
                    }

                    await connection[cmd].ObservePlayerAsync(targetPlayer);
                }
            }
        }
        private static bool IsIsland(string targetPlayerName)
        {
            return Equals(targetPlayerName, "home")
                         || Equals(targetPlayerName, "away")
                         || Equals(targetPlayerName, "ironhill")
                         || Equals(targetPlayerName, "heim")
                         || Equals(targetPlayerName, "kyo")
                         || Equals(targetPlayerName, "atria")
                         || Equals(targetPlayerName, "eldara");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(string str, string other)
        {
            return string.Equals(str, other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
