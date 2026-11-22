using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    ///     Outfits per skill: gear that follows what you train. !outfit sends "outfit_command" with what came after it;
    ///     the game answers. Games from 0.9.7 read it.
    /// </summary>
    public class Outfit : ChatCommandHandler
    {
        public override string Category => "Items";
        public override string Description => "Your outfit follows what you train: what you wore last for a skill comes back when you train it again, and for raids, dungeons and the tower you get your combat outfit. Alone, shows the outfit of what you train. save keeps what you wear now for it; pin also stops better gear from replacing it (unpin lets it again); a skill's name puts that skill's outfit on now; auto on or off turns the changing on or off; upgrade on or off decides whether better gear is put on by itself.";
        public override string UsageExample => "!outfit, !outfit save, !outfit pin, !outfit mining, !outfit auto off";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "save, pin, unpin, auto, upgrade, or a skill (attack, ranged, mining, all...).", "save", "pin", "unpin", "auto", "upgrade").NotRequired(),
            ChatCommandInput.Create("setting", "on or off, after auto or upgrade.", "on", "off").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            await connection[cmd].OutfitAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }

    public class Outfits : Outfit { }
}
