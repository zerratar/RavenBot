using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DuelCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public DuelCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
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

            var sub = cmd.Arguments?.Trim();
            if (string.IsNullOrEmpty(sub))
            {
                broadcaster.Broadcast(cmd.Sender.Username,
                    "To duel a player you need to specify their name. ex: '!duel JohnDoe', to accept or decline a duel request use '!duel accept' or '!duel decline'. You may also cancel an ongoing request by using '!duel cancel'");
                return;
            }

            var player = playerProvider.Get(cmd.Sender);
            if (sub.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game.CancelDuelRequestAsync(player);
            }
            else if (sub.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game.AcceptDuelRequestAsync(player);
            }
            else if (sub.Equals("decline", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game.DeclineDuelRequestAsync(player);
            }
            else
            {
                await this.game.DuelRequestAsync(player, playerProvider.Get(sub));
            }
        }
    }
}