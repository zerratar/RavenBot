using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    /// <summary>The skill trees (combat v2): each command sends "skills_command" with its action first; the game answers.</summary>
    public abstract class SkillTreeCommandBase : ChatCommandHandler
    {
        public override string Category => "Combat";

        /// <summary>"learn", "loadout", "respec", or nothing for !tree.</summary>
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
            await connection[cmd].SkillTreeAsync(player, string.IsNullOrEmpty(Action) ? arguments : (Action + " " + arguments).Trim());
        }
    }

    public class Tree : SkillTreeCommandBase
    {
        protected override string Action => "";
        public override string Description => "Your skill trees: on its own, the skill points you have to spend and your loadout of active skills. With a tree's name (Bulwark, Restoration, Might, Precision, Marksman, Elements, Vitality, Hunter, or its combat skill) it lists that tree's skills; with a skill's name it explains the skill.";
        public override string UsageExample => "!tree, !tree Might, !tree strength, !tree cleave";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("tree or skill", "A tree (Bulwark, Restoration, Might, Precision, Marksman, Elements, Vitality, Hunter) or a skill's name.").NotRequired(),
        };
    }

    public class SkillTree : Tree { }
    public class Talents : Tree { }

    public class Learn : SkillTreeCommandBase
    {
        protected override string Action => "learn";
        public override string Description => "Spends skill points on a skill (or one more rank of a passive). Points come from your combat levels; a skill also needs a level in its tree's combat skill. A new active skill goes into a free loadout slot by itself.";
        public override string UsageExample => "!learn cleave, !learn brute force";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("skill", "The skill's name."),
        };
    }

    public class Loadout : SkillTreeCommandBase
    {
        protected override string Action => "loadout";
        public override string Description => "Your active skills in combat, four slots (five with Battle Rhythm). Your character uses them by itself on their cooldowns, the first slot that fits first. On its own it shows the loadout.";
        public override string UsageExample => "!loadout, !loadout 1 cleave, !loadout clear 3";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("slot", "The slot number, or clear followed by a slot number to empty it.").NotRequired(),
            ChatCommandInput.Create("skill", "The active skill to put in the slot.").NotRequired(),
        };
    }

    public class Respec : SkillTreeCommandBase
    {
        protected override string Action => "respec";
        public override string Description => "Gives back every skill point. On its own it tells you the price (a free respec, a Tome of Forgetting, or coins); add confirm to do it. Not during raids, dungeons or duels.";
        public override string UsageExample => "!respec, !respec confirm";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("confirm", "Add confirm to give the points back.", "confirm").NotRequired(),
        };
    }
}
