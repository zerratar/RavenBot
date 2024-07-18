using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using ROBot.Core.GameServer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Auto : ChatCommandHandler
    {
        public override string Category => "Events";
        public override string Description => "Automatically trigger actions in the game.";
        public override System.Collections.Generic.IReadOnlyList<ChatCommandInput> Inputs { get; } = new System.Collections.Generic.List<ChatCommandInput>
        {
            //ChatCommandInput.Create("event", "What kind of event to interact with", "dungeon", "raid"),
            //ChatCommandInput.Create("action", "What kind of action", "join", "stop"),
        };
        public override Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            // Auto start new raids, dungeons every N minute or second? auto join? auto something? :)
            // Auto craft? auto cook? auto brew? auto something else? All interactions costs coins

            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session == null)
            {
                return Task.CompletedTask;
            }

            var connection = game.GetConnection(session);
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            // we need to use !auto <action: join|rest|craft|cook|brew|drink|use|consume> <arg/amount>
            if (string.IsNullOrEmpty(cmd.Arguments))
            {
                return Task.CompletedTask;
            }
            var player = session.Get(cmd);
            if (player == null)
            {
                return Task.CompletedTask;
            }

            var options = cmd.Arguments.Split(' ').Select(x => x?.Trim().ToLower()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (options.Length == 0)
            {
                return Task.CompletedTask;
            }

            // check if its a raid or dungeon action
            var action = options[0];
            var arguments = options.Length > 1 ? options[1..] : [];

            switch (action)
            {
                case "dungeon":
                    return connection[cmd].AutoJoinDungeonAsync(player, arguments.LastOrDefault());

                case "raid":
                    return connection[cmd].AutoJoinRaidAsync(player, arguments.LastOrDefault());

                case "rest":
                    return HandleAutoRestAsync(cmd, connection, player, arguments);

                case "eat":
                case "consume":
                case "use":
                case "drink":
                    return HandleAutoUseAsync(cmd, connection, player, arguments);
            }

            return Task.CompletedTask;
        }

        private Task HandleAutoUseAsync(ICommand cmd, IRavenfallConnection connection, User player, string[] arguments)
        {
            // here we require an item name and the amount or "stop|off".
            var arg = arguments.LastOrDefault();
            if (string.IsNullOrEmpty(arg) || arguments.Length <= 1)
            {
                return Task.CompletedTask;
            }

            var itemName = string.Join(" ", arguments[..^2]);
            // good! we have an amount
            if (int.TryParse(arg, out var amount))
            {
                // get the item name by joining whatever is left in the arguments list
                // since the item name may have spaces in the name.
                return connection[cmd].AutoUseAsync(player, amount);
            }

            if (itemName.Equals("stop", StringComparison.OrdinalIgnoreCase)
                || itemName.Equals("clear", StringComparison.OrdinalIgnoreCase)
                || itemName.Equals("reset", StringComparison.OrdinalIgnoreCase)
                || itemName.Equals("off", StringComparison.OrdinalIgnoreCase)
                || itemName.Equals("false", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("stop", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("clear", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("reset", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("off", StringComparison.OrdinalIgnoreCase)
                || arguments[^1].Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return connection[cmd].StopAutoUseAsync(player);
            }

            return connection[cmd].RequestAutoUseStatusAsync(player);
        }

        private static async Task HandleAutoRestAsync(ICommand cmd, IRavenfallConnection connection, User player, string[] arguments)
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

            if (arg.Equals("off", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("stop", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("clear", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("reset", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("false", StringComparison.OrdinalIgnoreCase))
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
