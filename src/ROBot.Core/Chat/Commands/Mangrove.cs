using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>The banana island's commands (the game's docs/banana-island.md): one packet to the game, the command first.</summary>
    internal static class Mangrove
    {
        public static async Task SendAsync(IBotServer game, ICommand cmd, string command)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            await connection[cmd].MangroveAsync(player, (command + " " + (cmd.Arguments ?? string.Empty)).Trim());
        }
    }

    public class Dig : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "Help dig up a pirate's treasure on the banana island (the Mangrove island) when someone has found a map. Everyone who digs shares the chest.";
        public override string UsageExample => "!dig";
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd) => Mangrove.SendAsync(game, cmd, "dig");
    }

    public class Banana : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "See how the monkeys' banana pile on the banana island stands. !banana feed gives them your own bananas and !banana golden a Golden Banana; well fed monkeys steal less and the island gives more exp.";
        public override string UsageExample => "!banana feed 20";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "feed (or give) hands the monkeys your own bananas, golden gives them one Golden Banana; leave it out to see the pile.", "feed", "golden").NotRequired(),
            ChatCommandInput.Create("amount", "With feed: how many bananas to give; leave it out for 10.").NotRequired(),
        };
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd) => Mangrove.SendAsync(game, cmd, "banana");
    }

    public class Bananas : Banana
    {
    }

    public class Regatta : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Race your own boat round the banana island: !regatta joins, and the first one opens the sign-up. Places pay coins and Sailing exp. Not racing? Meddle with kegs, wind and pranks, and the streamer or a moderator can wake the Kraken once a race.";
        public override string UsageExample => "!regatta wind zerratar";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave it out to join (or open) the race. leave drops out; start begins it now (streamer or moderator). During a race: keg throws a powder keg in a racer's way and wind fills a racer's sails, free at a random racer once a minute or 500 coins sooner; kraken wakes the Kraken (streamer or moderator, once a race); swim and propeller are free pranks.", "leave", "start", "keg", "wind", "kraken", "swim", "propeller").NotRequired(),
            ChatCommandInput.Create("racer", "With keg, wind, swim or propeller: the racer to aim at; leave it out for a random one. Aiming a keg or wind costs coins (2000, doubling each time you aim in the same race, up to 8000), and you send the command again to pay; half of every paid meddle goes to the prize pot.").NotRequired(),
        };
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd) => Mangrove.SendAsync(game, cmd, "regatta");
    }

    public class Pirates : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "The streamer or a moderator calls the pirates' raid on the banana island: three waves come for the banana pile.";
        public override string UsageExample => "!pirates";
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd) => Mangrove.SendAsync(game, cmd, "pirates");
    }
}
