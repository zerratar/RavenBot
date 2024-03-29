﻿using System;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RavenfallCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public RavenfallCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            var arg = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(arg))
            {
                return;
            }

            var player = playerProvider.Get(cmd);

            if (arg.StartsWith("update"))
            {
                await this.game[cmd.CorrelationId].UpdateGameAsync(player);
                return;
            }

            if (arg.StartsWith("help"))
            {
                await chat.SendReplyAsync(cmd, Localization.HELP);
                return;
            }

            if (arg.StartsWith("join"))
            {
                await this.game[cmd.CorrelationId].JoinAsync(player);
            }

            if (arg.StartsWith("task"))
            {
                var task = arg.Split(' ').LastOrDefault();

                var availableTasks = Enum.GetValues(typeof(PlayerTask))
                    .Cast<PlayerTask>()
                    .ToList();

                if (string.IsNullOrEmpty(task))
                {
                    await chat.SendReplyAsync(cmd, Localization.TASK_NO_ARG, string.Join(", ", availableTasks.Select(x => x.ToString())));
                    return;
                }

                var targetTask = availableTasks.FirstOrDefault(x =>
                    x.ToString().Equals(task, StringComparison.InvariantCultureIgnoreCase));

                await this.game[cmd.CorrelationId].SendPlayerTaskAsync(player, targetTask);
            }
        }
    }
}