using RavenBot.Core.Handlers;
using ROBot.Core.Chat.Twitch;
using ROBot.Core.GameServer;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Boat : ChatCommandHandler
    {
        public override string Category => "Sailing";
        public override string Description => "Your own boat: on its own, !boat says where your boat is and how fast it sails. The shipwright on Away builds boats from crafted parts and coins, other players can sail along as your guests, and you can change your boat's looks with the parts you have earned.";
        public override string UsageExample => "!boat, !boat build fishing boat, !boat list, !boat invite zerratar, !boat accept, !boat looks, !boat sails 3";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave it out to see your boat. build has the shipwright on Away build one (buy works too), list shows what each boat needs. invite asks a player aboard your boat, join asks to come aboard theirs, accept or decline answers a request, leave gets a guest off. look shows what your boat wears, looks what you have earned, and a part name changes that part (the parts depend on the boat).", "build", "list", "invite", "join", "accept", "decline", "leave", "look", "looks", "hull", "front", "middle", "back", "figurehead", "ram", "sails", "flag", "wood").NotRequired(),
            ChatCommandInput.Create("value", "For build: the boat, fishing boat, sloop, brigantine or galleon. For invite or join: the player's name. For a part: the look number, none to take it off, or clan (flag and sails) for your clan's logo.").NotRequired(),
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
                    if (player == null)
                        return;

                    await connection[cmd].BoatAsync(player, cmd.Arguments ?? string.Empty);
                }
            }
        }
    }
}
