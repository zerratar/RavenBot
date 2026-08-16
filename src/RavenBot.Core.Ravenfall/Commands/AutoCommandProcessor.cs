using System;
using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Commands
{

    public class AutoCommandProcessor : Net.RavenfallCommandProcessor
    {
        private readonly IRavenfallClient connection;
        private readonly IUserProvider playerProvider;

        public AutoCommandProcessor(IRavenfallClient connection, IUserProvider playerProvider)
        {
            this.connection = connection;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageChat chat, ICommand cmd)
        {
            if (!await this.connection.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                await chat.SendReplyAsync(cmd, Localization.GAME_NOT_STARTED);
                return;
            }

            var player = playerProvider.Get(cmd);
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                await this.connection[cmd.CorrelationId].JoinDungeonAsync(player, null);
                return;
            }

            // we need to use !auto <action: join|rest|craft|cook|brew|drink|use|consume> <arg/amount>
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                return;
            }
            var options = cmd.Arguments.Split(' ').Select(x => x?.Trim().ToLower()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (options.Length == 0)
            {
                return;
            }

            // check if its a raid or dungeon action
            var action = options[0];
            var arguments = options.Length > 1 ? options[1..] : [];

            if (action.Equals("join", StringComparison.OrdinalIgnoreCase) && arguments != null && arguments.Length > 0)
            {
                var lastArgument = arguments.LastOrDefault();
                if (arguments[0].Equals("dungeon"))
                {
                    await connection[cmd].AutoJoinDungeonAsync(player, arguments.LastOrDefault());
                    return;
                }

                if (arguments[0].Equals("raid"))
                {
                    await connection[cmd].AutoJoinDungeonAsync(player, arguments.LastOrDefault());
                    return;
                }
            }
            switch (action)
            {
                case "dungeon":
                    await this.connection[cmd.CorrelationId].AutoJoinDungeonAsync(player, arguments.LastOrDefault());
                    return;


                case "raid":
                    await this.connection[cmd.CorrelationId].AutoJoinRaidAsync(player, arguments.LastOrDefault());
                    return;

                case "rest":
                    await HandleAutoRestAsync(cmd, connection, player, arguments);
                    return;

                case "eat":
                case "consume":
                case "use":
                case "drink":
                    await HandleAutoUseAsync(cmd, connection, player, arguments);
                    return;
            }
        }

        private async Task HandleAutoUseAsync(ICommand cmd, IRavenfallClient connection, User player, string[] arguments)
        {
            // here we require an item name and the amount or "stop|off".
            var arg = arguments.LastOrDefault();
            if (string.IsNullOrEmpty(arg) || arguments.Length <= 1)
            {
                return;
            }

            var itemName = string.Join(" ", arguments[..^2]);
            // good! we have an amount
            if (int.TryParse(arg, out var amount))
            {
                // get the item name by joining whatever is left in the arguments list
                // since the item name may have spaces in the name.
                await connection[cmd].AutoUseAsync(player, amount);
                return;
            }

            if (itemName.Equals("stop", StringComparison.OrdinalIgnoreCase)
                || itemName.Equals("off", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("stop", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                await connection[cmd].StopAutoUseAsync(player);
                return;
            }

            await connection[cmd].RequestAutoUseStatusAsync(player);
        }

        private async Task HandleAutoRestAsync(ICommand cmd, IRavenfallClient connection, User player, string[] arguments)
        {
            var startRestTime = 0;
            var endRestTime = 120;

            // when auto resting you will need to provide: "start rest time" and "stop rest time"
            // the "start rest time" is when the player should start resting (default: 0)
            // the "stop rest time" is when the player should stop resting and start training (default: 120)
            var arg = arguments.LastOrDefault();
            // if no argument is provided, we will use the default settings.
            if (arg == null)
            {
                await connection[cmd].AutoRestAsync(player, startRestTime, endRestTime);
                return;
            }

            if (arg.Equals("off", StringComparison.OrdinalIgnoreCase) || arg.Equals("reset", StringComparison.OrdinalIgnoreCase) || arg.Equals("clear", StringComparison.OrdinalIgnoreCase) || arg.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                await connection[cmd].StopAutoRestAsync(player);
                return;
            }

            if (arg.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                await connection[cmd].RequestAutoRestStatusAsync(player);
                return;
            }


            if (arguments.Length == 1)
            {
                int.TryParse(arguments[0], out endRestTime);
            }
            else if (arguments.Length > 1)
            {
                int.TryParse(arguments[0], out startRestTime);
                int.TryParse(arguments[1], out endRestTime);
            }

            await connection[cmd].AutoRestAsync(player, startRestTime, endRestTime);
        }
    }
}