﻿using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class OnsenCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public OnsenCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
        {
            this.RequiresBroadcaster = true;
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


            var leaveOnsen = !string.IsNullOrEmpty(cmd.Arguments) && cmd.Arguments.Contains("leave", StringComparison.OrdinalIgnoreCase);
            if (leaveOnsen)
            {
                await this.game[cmd.CorrelationId].LeaveOnsenAsync(player);
            }
            else
            {
                await this.game[cmd.CorrelationId].JoinOnsenAsync(player);
            }
        }
    }
}