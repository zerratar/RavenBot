using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
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
        public override string Description => "How the monkeys' banana pile on the banana island stands. !banana feed <amount> gives them your own bananas, !banana golden a Golden Banana. Well fed monkeys steal less and the island gives more exp.";
        public override string UsageExample => "!banana, !banana feed 20, !banana golden";
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd) => Mangrove.SendAsync(game, cmd, "banana");
    }

    public class Bananas : Banana
    {
    }

    public class Regatta : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Race your own boat round the banana island. The first !regatta opens the sign-up; the streamer can start it at once with !regatta start. Places pay coins and Sailing exp. Not racing? Meddle: !regatta keg throws a powder keg in front of a racer, !regatta wind fills a racer's sails (free once a minute, add a name to aim it for coins); the streamer can wake the Kraken once a race with !regatta kraken. Just for laughs, free: !regatta swim sends a racer overboard to tow their boat, !regatta propeller makes them its propeller.";
        public override string UsageExample => "!regatta, !regatta start, !regatta leave, !regatta keg, !regatta wind <name>, !regatta kraken, !regatta swim <name>, !regatta propeller <name>";
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
