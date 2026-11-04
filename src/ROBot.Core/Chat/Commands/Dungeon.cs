using Discord;
using RavenBot.Core.Handlers;
using ROBot.Core.GameServer;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Dungeon : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "Dungeons: !dungeon on its own joins the dungeon that is open. You can also open one with a dungeon scroll, have your character join them automatically, and pick the combat skill you train in dungeons.";
        public override string UsageExample => "!dungeon, !dungeon start, !dungeon join on, !dungeon skill magic";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "Leave it out to join. start uses one of your dungeon scrolls to open a dungeon; join on its own joins too, and join (or auto) with a value sets auto join, which costs coins each time; skill shows or sets the combat skill you train in dungeons; stop ends the running dungeon (streamer, moderators and game staff); proceed and kill boss are game admin tools.", "start", "join", "skill", "stop", "proceed", "kill boss").NotRequired(),
            ChatCommandInput.Create("value", "For join: on, off, status, or how many dungeons to join. For skill: attack, defense, strength, ranged, magic, healing or all, or reset to clear it; leave it out to see your current one.").NotRequired(),
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

                    if (string.IsNullOrEmpty(cmd.Arguments))
                    {
                        await connection[cmd].JoinDungeonAsync(player, null);
                        return;
                    }

                    // the first word picks the action; it was matched anywhere in the text
                    var words = cmd.Arguments.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    var action = words.Length > 0 ? words[0].ToLowerInvariant() : string.Empty;
                    var rest = string.Join(" ", words.Skip(1)).ToLowerInvariant();
                    if (action == "join" || action == "auto")
                    {
                        // !dungeon join on its own joins; auto on its own tells the auto join setting
                        if (rest.Length == 0 && action == "join") await connection[cmd].JoinDungeonAsync(player, null);
                        else await connection[cmd].AutoJoinDungeonAsync(player, rest.Length == 0 ? "status" : words.Last());
                    }
                    else if (action == "proceed" || (action == "next" && rest == "room"))
                    {
                        await connection[cmd].ProceedDungeonAsync(player);
                    }
                    else if (action == "kill" && rest == "boss")
                    {
                        await connection[cmd].KillDungeonBossAsync(player);
                    }
                    else if (action == "stop")
                    {
                        // game moderators and administrators too, as the game allows
                        if (cmd.Sender.IsBroadcaster || cmd.Sender.IsModerator || cmd.Sender.IsGameModerator || cmd.Sender.IsGameAdmin)
                        {
                            await connection[cmd].StopDungeonAsync(player);
                        }
                    }
                    else if (action == "start")
                    {
                        await connection[cmd].DungeonStartAsync(player);
                    }
                    else if ((action == "skill" || action == "style") && rest.Length == 0)
                    {
                        await connection[cmd].GetDungeonCombatStyleAsync(player);
                    }
                    else if (action == "skill" || action == "style")
                    {
                        var targetSkill = words[1];
                        if (targetSkill.Equals("reset", System.StringComparison.OrdinalIgnoreCase) || targetSkill.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
                        {
                            await connection[cmd].ClearDungeonCombatStyleAsync(player);
                        }
                        else
                        {
                            await connection[cmd].SetDungeonCombatStyleAsync(player, targetSkill);
                        }
                    }
                }
            }
        }
    }
}
