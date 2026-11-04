using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Disenchant : ChatCommandHandler
    {
        public override string Category => "Skills";
        public override string Description => "Take the enchantment off an item, turning it back into the plain item. Same as !enchant remove.";
        public override string UsageExample => "!disenchant rune 2h sword, !disenchant last";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The enchanted item. Leave it out for the item you enchanted in the last two minutes, or type last for the last one you enchanted.").NotRequired(),
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
                    var player = session.Get(cmd);
                    if (player != null)
                        await connection[cmd].DisenchantAsync(player, cmd.Arguments);
                }
            }
        }
    }
}
