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

            if (!await game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);

            var item = cmd.Arguments?.ToLower();
            if (!string.IsNullOrEmpty(item))
            {
                if (item == "remove cooldown" || item == "clear cooldown" || item == "remove cd" || item == "clear cd")
                {
                    await game[cmd.CorrelationId].ClearEnchantmentCooldownAsync(player);
                    return;
                }

                if (item == "cooldown" || item == "cd")
                {
                    await game[cmd.CorrelationId].GetEnchantmentCooldownAsync(player);
                    return;
                }

                if (item.Split(' ')[0] == "remove")
                {
                    await game[cmd.CorrelationId].DisenchantAsync(player, item.Replace("remove", "").Trim());
                    return;
                }
            }
            //    broadcaster.Broadcast(cmd.Sender.Username, "You have to use !equip <item name> or !equip all for equipping your best items.");
            //    return;
            //}

            await game[cmd.CorrelationId].EnchantAsync(player, item);
        }
    }
}