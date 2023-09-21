using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DisenchantItemCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public DisenchantItemCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await chat.SendReplyAsync(cmd, "{command} <item>", cmd.Command);
                return;
            }

            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].DisenchantAsync(player, cmd.Arguments);
        }
    }
}