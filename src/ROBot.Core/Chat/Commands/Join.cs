using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Join : ChatCommandHandler
    {
        public override string Category => "Game";
        public override string Description => "Use this command to start playing!";
        public override string UsageExample => "!join main";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("character", "Which character do you want to play with?")
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
                    var player = session.Join(cmd.Sender, cmd.Arguments);
                    await connection[cmd].JoinAsync(player);
                }
            }
            else if (chat is Discord.DiscordCommandClient) // only for discord
            {
                await chat.SendReplyAsync(cmd, "There are currently no active Ravenfall game sessions in this channel.");
            }
        }
    }
}
