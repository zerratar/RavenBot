using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Toggle : ChatCommandHandler
    {
        public override string Category => "Appearance";
        public override string Description => "Show or hide your helmet or your cape, or switch to the next pet you own.";
        public override string UsageExample => "!toggle helmet";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("target", "helmet shows or hides your helmet, cape shows or hides your cape (your own or your clan's), pet cycles to your next pet.", "helmet", "cape", "pet").Required()
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
                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await chat.SendReplyAsync(cmd, Localization.VALUE_NO_ARG, cmd.Command);
                        return;
                    }

                    var player = session.Get(cmd);
                    if (cmd.Arguments.Contains("helm", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].ToggleHelmetAsync(player);
                    }
                    else if (cmd.Arguments.Contains("cape", StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("cloak", StringComparison.OrdinalIgnoreCase))
                    {
                        // the game answers: the cape hides or shows, and a hidden cape lets the armor's own cloak or pack show
                        await connection[cmd].ToggleCapeAsync(player);
                    }
                    else if (cmd.Arguments.Contains("pet", StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].TogglePetAsync(player);
                    }
                    else
                    {
                        await chat.SendReplyAsync(cmd, Localization.TOGGLE_INVALID, cmd.Arguments);
                    }
                }
            }
        }
    }
}
