using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class VendorItemCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public VendorItemCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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
                await chat.SendReplyAsync(cmd, "{command} <item> (optional: <amount>, default 1)", cmd.Command);
                return;
            }

            var player = playerProvider.Get(cmd);
            await this.game[cmd.CorrelationId].VendorItemAsync(player, cmd.Arguments);
        }
    }
}