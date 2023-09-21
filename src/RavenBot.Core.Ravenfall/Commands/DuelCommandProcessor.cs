using System;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DuelCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public DuelCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            var sub = cmd.Arguments?.Trim();
            if (string.IsNullOrEmpty(sub))
            {
                await chat.SendReplyAsync(cmd,
                    "To duel a player you need to specify their name. ex: '!duel JohnDoe', to accept or decline a duel request use '!duel accept' or '!duel decline'. You may also cancel an ongoing request by using '!duel cancel'");
                return;
            }

            var player = playerProvider.Get(cmd);
            if (sub.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game[cmd.CorrelationId].CancelDuelRequestAsync(player);
            }
            else if (sub.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game[cmd.CorrelationId].AcceptDuelRequestAsync(player);
            }
            else if (sub.Equals("decline", StringComparison.InvariantCultureIgnoreCase))
            {
                await this.game[cmd.CorrelationId].DeclineDuelRequestAsync(player);
            }
            else
            {
                await this.game[cmd.CorrelationId].DuelRequestAsync(player, playerProvider.Get(sub));
            }
        }
    }
}