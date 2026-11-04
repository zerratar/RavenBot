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
            ChatCommandInput.Create("action", "Leave it out to join. start uses one of your dungeon scrolls to open a dungeon; join (or auto) sets auto join, which costs coins each time; skill shows or sets the combat skill you train in dungeons; stop ends the running dungeon (streamer and moderators); proceed and kill boss are game admin tools.", "start", "join", "skill", "stop", "proceed", "kill boss").NotRequired(),
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
                    else if (cmd.Arguments.Contains("join ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("auto ", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].AutoJoinDungeonAsync(player, cmd.Arguments.Split(' ').LastOrDefault());
                    }
                    else if (cmd.Arguments.Contains("proceed", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("next room", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].ProceedDungeonAsync(player);
                    }
                    else if (cmd.Arguments.Contains("kill boss", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].KillDungeonBossAsync(player);
                    }
                    else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (player.IsBroadcaster || player.IsModerator)
                        {
                            await connection[cmd].StopDungeonAsync(player);
                        }
                    }
                    else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].DungeonStartAsync(player);
                    }
                    else if (cmd.Arguments.Equals("skill", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await connection[cmd].GetDungeonCombatStyleAsync(player);
                    }
                    else if (cmd.Arguments.Contains("skill ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("style ", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var targetSkill = cmd.Arguments.Split(' ').Skip(1).FirstOrDefault();
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
