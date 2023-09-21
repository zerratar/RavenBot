using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Requirement : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Check what the crafting requirements are for a target item.";
        public override string UsageExample => "!requirement rune 2h sword";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "Which item do you want to check?").Required(),
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
                    if (player != null)
                        await connection[cmd].CraftRequirementAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
