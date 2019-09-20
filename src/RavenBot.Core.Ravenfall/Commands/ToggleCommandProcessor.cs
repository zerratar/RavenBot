using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ToggleCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ToggleCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                //broadcaster.Broadcast(

                broadcaster.Send(cmd.Sender.Username,
                    Localization.GAME_NOT_STARTED);
                return;
            }

            if (string.IsNullOrEmpty(cmd.Arguments))
            {

                broadcaster.Send(cmd.Sender.Username,
                    "You need to specify what to toggle, like: helm or pet");
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
                broadcaster.Send(cmd.Sender.Username, cmd.Arguments + " cannot be toggled.");
            }
        }
    }
}