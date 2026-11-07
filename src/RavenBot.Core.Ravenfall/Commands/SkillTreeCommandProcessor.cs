using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>
    /// The skill trees (combat v2): !tree (points and loadout), !tree Might (a tree's skills), !tree cleave (one skill),
    /// !learn cleave, !loadout 1 cleave and !respec. The game answers them all; each command sends its action first.
    /// </summary>
    public abstract class SkillTreeCommandProcessorBase : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        protected SkillTreeCommandProcessorBase(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        /// <summary>The action in front of what the player wrote: "learn", "loadout", "respec", or nothing for !tree.</summary>
        protected abstract string Action { get; }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            var arguments = (cmd.Arguments ?? string.Empty).Trim();
            await this.game[cmd.CorrelationId].SkillTreeAsync(player, string.IsNullOrEmpty(Action) ? arguments : (Action + " " + arguments).Trim());
        }
    }

    public class SkillTreeCommandProcessor : SkillTreeCommandProcessorBase
    {
        public SkillTreeCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "";
    }

    public class LearnCommandProcessor : SkillTreeCommandProcessorBase
    {
        public LearnCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "learn";
    }

    public class LoadoutCommandProcessor : SkillTreeCommandProcessorBase
    {
        public LoadoutCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "loadout";
    }

    public class RespecCommandProcessor : SkillTreeCommandProcessorBase
    {
        public RespecCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "respec";
    }
}
