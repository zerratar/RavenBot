using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class DungeonCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IUserProvider playerProvider;

        public DungeonCommandProcessor(IRavenfallClient game, IUserProvider playerProvider)
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

            var player = playerProvider.Get(cmd);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.game[cmd.CorrelationId].JoinDungeonAsync(player, null);
                return;
            }
            else if (cmd.Arguments.Contains("join ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("auto ", System.StringComparison.OrdinalIgnoreCase))
            {
                await game[cmd.CorrelationId].AutoJoinDungeonAsync(player, cmd.Arguments.Split(' ').LastOrDefault());
            }
            else if (cmd.Arguments.Contains("stop", System.StringComparison.OrdinalIgnoreCase))
            {
                if (player.IsBroadcaster || player.IsModerator)
                {
                    await this.game[cmd.CorrelationId].StopDungeonAsync(player);
                }
            }
            else if (cmd.Arguments.Contains("proceed", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("next room", System.StringComparison.OrdinalIgnoreCase))
            {
                await game[cmd].ProceedDungeonAsync(player);
            }
            else if (cmd.Arguments.Contains("kill boss", System.StringComparison.OrdinalIgnoreCase))
            {
                await game[cmd].KillDungeonBossAsync(player);
            }
            else if (cmd.Arguments.Contains("start", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game[cmd.CorrelationId].DungeonStartAsync(player);
            }
            else if (cmd.Arguments.Equals("skill", System.StringComparison.OrdinalIgnoreCase))
            {
                await this.game[cmd.CorrelationId].GetDungeonCombatStyleAsync(player);
            }
            else if (cmd.Arguments.Contains("skill ", System.StringComparison.OrdinalIgnoreCase) || cmd.Arguments.Contains("style ", System.StringComparison.OrdinalIgnoreCase))
            {
                var targetSkill = cmd.Arguments.Split(' ').Skip(1).FirstOrDefault();
                if (targetSkill.Equals("reset", System.StringComparison.OrdinalIgnoreCase) || targetSkill.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
                {
                    await game[cmd].ClearDungeonCombatStyleAsync(player);
                }
                else
                {
                    await game[cmd].SetDungeonCombatStyleAsync(player, targetSkill);
                }
            }
        }
    }
}