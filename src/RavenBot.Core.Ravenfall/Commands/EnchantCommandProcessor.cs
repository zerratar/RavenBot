﻿using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class EnchantCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;
        public EnchantCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {

            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd.Sender);


            var item = cmd.Arguments?.ToLower();
            if (!string.IsNullOrEmpty(item) && item.Split(' ')[0] == "remove")
            {
                await game.DisenchantAsync(player, item.Replace("remove", "").Trim());
                return;
            }
            //    broadcaster.Broadcast(cmd.Sender.Username, "You have to use !equip <item name> or !equip all for equipping your best items.");
            //    return;
            //}

            await game.EnchantAsync(player, item);
        }
    }
}