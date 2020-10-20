using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ToggleCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ToggleCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                broadcaster.Broadcast(cmd.Sender.Username, "You need to specify what to toggle, like: helm or pet");
                return;
            }

            if (cmd.Arguments.Contains("helm", StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd.Sender);
                await game.ToggleHelmetAsync(player);
            }
            else if (cmd.Arguments.Contains("pet", StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd.Sender);
                await game.TogglePetAsync(player);
            }
            else
            {
                broadcaster.Broadcast(cmd.Sender.Username, "{invalidOption} cannot be toggled.", cmd.Arguments);
            }
        }
    }
}