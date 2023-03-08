﻿using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class SailCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public SailCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            if (cmd.Command.StartsWith("disembark"))
            {
                await game.DisembarkFerryAsync(player);
                return;
            }

            var destination = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(destination))
            {
                await game.EmbarkFerryAsync(player);
                return;
            }

            if (destination.StartsWith("stop"))
            {
                await game.DisembarkFerryAsync(player);
                return;
            }

            await game.TravelAsync(player, destination);
        }
    }
}