using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>Gems and sockets (combat v2): each command sends "gems_command" with its action first; the game answers.</summary>
    public abstract class GemsCommandBase : ChatCommandHandler
    {
        public override string Category => "Items";

        /// <summary>"socket", "unsocket", or nothing for !gems (whose own first word is the action).</summary>
        protected abstract string Action { get; }

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            var arguments = (cmd.Arguments ?? string.Empty).Trim();
            await connection[cmd].GemsAsync(player, string.IsNullOrEmpty(Action) ? arguments : (Action + " " + arguments).Trim());
        }
    }

    public class Gems : GemsCommandBase
    {
        protected override string Action => "";
        public override string Description => "Your enchanted gems. With enchant and a raw gem, your clan's Enchanting turns mined gems into gems for sockets (the clan's level gives better tiers). With combine, three gems of a tier become one of the next tier, for coins. With a gem's name, what it does in a weapon, in armour and in a ring or amulet.";
        public override string UsageExample => "!gems, !gems enchant ruby 5, !gems combine ruby flawed, !gems diamond";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "enchant, combine, or a gem's name (Ruby, Sapphire, Topaz, Emerald, Diamond, Onyx, Amethyst).").NotRequired(),
            ChatCommandInput.Create("gem", "The gem, and for combine its tier (Chipped, Flawed, Polished, Flawless).").NotRequired(),
            ChatCommandInput.Create("amount", "How many to enchant or combine.").NotRequired(),
        };
    }

    public class Gem : Gems { }

    public class Socket : GemsCommandBase
    {
        protected override string Action => "socket";
        public override string Description => "Puts a gem into an item's socket: weapons, armour, rings and amulets from item level 25 have one (2 from 150, 3 from 400; rings and amulets 1). The best gem of that kind you own is used unless you name a tier. A socket number at the end replaces the gem in it, which crumbles unless the keep roll succeeds.";
        public override string UsageExample => "!socket rune sword ruby, !socket rune sword flawless ruby 2";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item's name."),
            ChatCommandInput.Create("gem", "The gem, a tier before it if you like (flawless ruby)."),
            ChatCommandInput.Create("socket", "A socket number, to replace the gem in it.").NotRequired(),
        };
    }

    public class Unsocket : GemsCommandBase
    {
        protected override string Action => "unsocket";
        public override string Description => "Takes a gem out of an item. It crumbles unless you are lucky: your clan's Enchanting level gives the chance to keep it (from level 100, up to 40% at 400).";
        public override string UsageExample => "!unsocket rune sword, !unsocket rune sword 2";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("item", "The item's name."),
            ChatCommandInput.Create("socket", "Which socket (the last filled one if left out).").NotRequired(),
        };
    }
}
