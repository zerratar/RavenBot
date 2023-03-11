using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class EnchantCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public EnchantCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {

            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                chat.SendReply(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);


            var item = cmd.Arguments?.ToLower();
            if (!string.IsNullOrEmpty(item) && item.Split(' ')[0] == "remove")
            {
                await this.game.Reply(cmd.CorrelationId).DisenchantAsync(player, item.Replace("remove", "").Trim());
                return;
            }
            //    broadcaster.Broadcast(cmd.Sender.Username, "You have to use !equip <item name> or !equip all for equipping your best items.");
            //    return;
            //}

            await this.game.Reply(cmd.CorrelationId).EnchantAsync(player, item);
        }
    }
}