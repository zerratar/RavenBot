using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class UploadCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public UploadCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
            var type = cmd.Arguments?.ToLowerInvariant() ?? "state";
            await this.game[cmd.CorrelationId].UploadAsync(player, type);
        }
    }

}