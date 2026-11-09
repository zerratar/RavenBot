using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>
    ///     Battle pets: !pets sends "pet_command" with its action first, !tame sends "tame_command"; the game answers.
    ///     !pet stays the cosmetic pet.
    /// </summary>
    public class Pets : ChatCommandHandler
    {
        public override string Category => "Pets";
        public override string Description => "Your battle pets. Alone, the pet that is out with you. starter (fox, bear or fawn) gives you a first pet; stable lists them all; use (name) sends one out and away puts it back; rename, skills and slot (number) (skill) change it; evolve evolves it when it is ready; wake (pet item) wakes a pet item you have so it fights too; hatch (egg) puts an egg in your stable to hatch while you play; about (kind) tells you about a kind of pet.";
        public override string UsageExample => "!pets, !pets stable, !pets use Bun, !pets slot 2 howl, !pets evolve, !pets wake diamond raven, !pets about bunny";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "starter, stable, use, away, rename, skills, slot, evolve, wake, hatch, release or about.", "starter", "stable", "use", "away", "rename", "skills", "slot", "evolve", "wake", "hatch", "release", "about").NotRequired(),
            ChatCommandInput.Create("details", "A pet's name or number, a new name, a slot and a skill, a pet item or a kind of pet.").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            await connection[cmd].PetsAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }

    public class BattlePet : Pets { }

    public class Tame : ChatCommandHandler
    {
        public override string Category => "Pets";
        public override string Description => "Tames a kind of creature: while you fight, each one you defeat is a try, and every try trains Taming. Creatures only (bunnies, cows, Tormented Souls, metalons, dragons...), never people. Alone, says what you are taming; stop ends it.";
        public override string UsageExample => "!tame bunny, !tame metalon, !tame red dragon, !tame stop";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("creature", "The kind of creature, or stop.").NotRequired(),
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var session = game.GetSession(cmd.Channel);
            if (session == null) return;
            var connection = game.GetConnection(session);
            if (connection == null) return;
            var player = session.Get(cmd);
            if (player == null) return;
            await connection[cmd].TameAsync(player, (cmd.Arguments ?? string.Empty).Trim());
        }
    }
}
