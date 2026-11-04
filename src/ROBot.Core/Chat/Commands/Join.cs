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
        public override string Description => "Join the game and start playing! You have up to three characters; name one to play it, or leave it out to play your first.";
        public override string UsageExample => "!join 2";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("character", "Which character to play: its number (1, 2 or 3) or the alias you gave it; leave it out for your first character.").NotRequired()
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
