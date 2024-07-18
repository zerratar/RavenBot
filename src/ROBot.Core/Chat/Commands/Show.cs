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
        public override string Description => "Allows for focusing the camera on your character.";
        public override string UsageExample => "!show";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "If you're a broadcaster or moderator you can observe a target player or target island.")
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
