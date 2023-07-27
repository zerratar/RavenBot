using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Pet : ChatCommandHandler
    {
        public override string Category => "Appearance";
        public override string Description => "This command allows you set your active pet";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("pet", "The pet you want to use").Required()
        };

        public override string UsageExample => "!pet blue orb pet";
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
                    if (player == null)
                        return;

                    var pet = cmd.Arguments?.ToLower();
                    if (string.IsNullOrEmpty(pet))
                    {
                        await connection[cmd].GetPetAsync(player);
                        return;
                    }

                    await connection[cmd].SetPetAsync(player, pet);
                }
            }
        }
    }
}
