using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ToggleCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public ToggleCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                chat.SendReply(cmd, Localization.TOGGLE_NO_ARG);
                return;
            }

            if (cmd.Arguments.Contains("helm", StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd);
                await this.game.Reply(cmd.CorrelationId).ToggleHelmetAsync(player);
            }
            else if (cmd.Arguments.Contains("pet", StringComparison.OrdinalIgnoreCase))
            {
                var player = playerProvider.Get(cmd);
                await this.game.Reply(cmd.CorrelationId).TogglePetAsync(player);
            }
            else
            {
                chat.SendReply(cmd, Localization.TOGGLE_INVALID, cmd.Arguments);
            }
        }
    }
}