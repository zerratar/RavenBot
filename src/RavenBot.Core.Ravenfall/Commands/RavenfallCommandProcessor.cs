﻿using System;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
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

        public override async Task ProcessAsync(IMessageChat broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.GAME_NOT_STARTED);
                return;
            }

            var arg = cmd.Arguments?.ToLower();
            if (string.IsNullOrEmpty(arg))
            {
                return;
            }

            if (arg.StartsWith("help"))
            {
                broadcaster.Broadcast(cmd.Sender.Username, Localization.HELP);
                return;
            }

            if (arg.StartsWith("join"))
            {
                var player = playerProvider.Get(cmd.Sender);
                await game.JoinAsync(player);
            }

            if (arg.StartsWith("task"))
            {
                var player = playerProvider.Get(cmd.Sender);
                var task = arg.Split(' ').LastOrDefault();

                var availableTasks = Enum.GetValues(typeof(PlayerTask))
                    .Cast<PlayerTask>()
                    .ToList();

                if (string.IsNullOrEmpty(task))
                {
                    broadcaster.Broadcast(cmd.Sender.Username, Localization.TASK_NO_ARG, string.Join(", ", availableTasks.Select(x => x.ToString())));
                    return;
                }

                var targetTask = availableTasks.FirstOrDefault(x =>
                    x.ToString().Equals(task, StringComparison.InvariantCultureIgnoreCase));

                await game.SendPlayerTaskAsync(player, targetTask);
            }
        }
    }
}