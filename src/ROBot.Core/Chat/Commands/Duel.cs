using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Duel : ChatCommandHandler
    {
        public override string Category => "PvP";
        public override string Description => "This command allows for duelling other players";
        public override string UsageExample => "!duel zerratar";

        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "What kind of action you want to take with an ongoing duel request", "cancel", "accept", "decline"),
            ChatCommandInput.Create("player", "The target player you want to duel")
        };

        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd);
                    if (string.IsNullOrWhiteSpace(cmd.Arguments))
                    {
                        //twitch.Broadcast(cmd.Sender.Username, "To duel a player you need to specify their name. ex: '!duel JohnDoe', to accept or decline a duel request use '!duel accept' or '!duel decline'. You may also cancel an ongoing request by using '!duel cancel'");
                        return;
                    }

                    var sub = cmd.Arguments?.Trim();
                    if (sub.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection[cmd].CancelDuelRequestAsync(player);
                    }
                    else if (sub.Equals("accept", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection[cmd].AcceptDuelRequestAsync(player);
                    }
                    else if (sub.Equals("decline", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await connection[cmd].DeclineDuelRequestAsync(player);
                    }
                    else
                    {
                        await connection[cmd].DuelRequestAsync(player, session.GetUserByName(sub));
                    }
                }
            }
        }
    }
}
