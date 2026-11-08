using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    /// <summary>
    /// Gems and sockets (combat v2): !gems (enchant, combine, a gem's name), !socket and !unsocket. The game answers them
    /// all; each command sends its action first.
    /// </summary>
    public abstract class GemsCommandProcessorBase : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        protected GemsCommandProcessorBase(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

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
            await this.game[cmd.CorrelationId].GemsAsync(player, string.IsNullOrEmpty(Action) ? arguments : (Action + " " + arguments).Trim());
        }
    }

    public class GemsCommandProcessor : GemsCommandProcessorBase
    {
        public GemsCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "";
    }

    public class SocketCommandProcessor : GemsCommandProcessorBase
    {
        public SocketCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "socket";
    }

    public class UnsocketCommandProcessor : GemsCommandProcessorBase
    {
        public UnsocketCommandProcessor(IRavenfallClient game, IUserProvider playerProvider) : base(game, playerProvider) { }
        protected override string Action => "unsocket";
    }
}
