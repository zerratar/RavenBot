﻿using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class PetCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;
        public PetCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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


            var pet = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(pet))
            {
                await game.GetPetAsync(player);
                return;
            }

            await game.SetPetAsync(player, pet);
        }
    }
}