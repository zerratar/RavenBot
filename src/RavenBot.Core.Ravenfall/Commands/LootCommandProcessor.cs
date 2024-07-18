using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class LootCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient connection;
        private readonly IUserProvider playerProvider;

        public LootCommandProcessor(IRavenfallClient connection, IUserProvider playerProvider)
        {
            this.connection = connection;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.connection.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            await this.connection[cmd.CorrelationId].GetLootAsync(player, cmd.Arguments?.Trim());
        }
    }
}