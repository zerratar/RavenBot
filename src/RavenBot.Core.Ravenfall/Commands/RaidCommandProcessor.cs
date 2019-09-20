using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class RaidCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public RaidCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            var player = playerProvider.Get(cmd.Sender);
            if (!string.IsNullOrEmpty(cmd.Arguments) && cmd.Arguments.Contains("start", StringComparison.OrdinalIgnoreCase))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                {
                    //broadcaster.Broadcast(

                    broadcaster.Send(cmd.Sender.Username,
                            Localization.PERMISSION_DENIED);
                    return;
                }

                await this.game.RaidStartAsync(player);
                return;
            }

            await this.game.JoinRaidAsync(player);
        }
    }
}