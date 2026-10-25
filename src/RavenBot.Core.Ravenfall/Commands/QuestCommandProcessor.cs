using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class QuestCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public QuestCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            // the local bot ships with the game, so it is always the game's own version
            await this.game[cmd.CorrelationId].QuestAsync(player, ArgumentsFor(cmd));
        }

        /// <summary>What the game is sent after quest_command: the chat's words, or a sub command first (!title, !achievements).</summary>
        protected virtual string ArgumentsFor(ICommand cmd) => cmd.Arguments ?? string.Empty;
    }

    /// <summary>!title: the titles earned, and !title 2 or !title off (!quest title).</summary>
    public class TitleCommandProcessor : QuestCommandProcessor
    {
        public TitleCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider)
        {
        }

        protected override string ArgumentsFor(ICommand cmd) => ("title " + (cmd.Arguments ?? string.Empty)).Trim();
    }

    /// <summary>!ravebridge: the Ravebridge event (!quest ravebridge): join, leave, follow, heal, revive, start, end.</summary>
    public class RavebridgeCommandProcessor : QuestCommandProcessor
    {
        public RavebridgeCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider)
        {
        }

        protected override string ArgumentsFor(ICommand cmd) => ("ravebridge " + (cmd.Arguments ?? string.Empty)).Trim();
    }

    /// <summary>!achievements: the medals reached and the closest next ones (!quest achievements).</summary>
    public class AchievementsCommandProcessor : QuestCommandProcessor
    {
        public AchievementsCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider)
        {
        }

        protected override string ArgumentsFor(ICommand cmd) => "achievements";
    }
}
