using Microsoft.IdentityModel.Tokens;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Commands;
using RavenBot.Core.Ravenfall.Models;
using System.Linq;

namespace RavenBot.Core.Ravenfall
{
    public class CommandProvider : ICommandProvider
    {
        private readonly IPlayerProvider playerProvider;
        private readonly ICommandHandler commandHandler;

        public CommandProvider(
            IPlayerProvider playerProvider,
            ICommandHandler commandHandler)
        {
            this.playerProvider = playerProvider;
            this.commandHandler = commandHandler;
        }

        public ICommand GetCommand(Player redeemer, string command, string arguments)
        {
            try
            {
                var broadcaster = playerProvider.GetBroadcaster();
                var cmdParts = command.ToLower().Split(' ');
                var processors = commandHandler.GetCommandProcessors();
                var cmd = cmdParts.FirstOrDefault(x => x.Contains("["));
                var usedCommand = cmd;

                if (!string.IsNullOrEmpty(cmd))
                    cmd = cmd.Replace("[", "").Replace("]", "").Trim();

                ICommandProcessor processor = null;


                if (string.IsNullOrEmpty(cmd))
                {
                    foreach (var part in cmdParts)
                    {
                        if (processors.TryGetValue(part.ToLower(), out processor))
                        {
                            usedCommand = part;
                            break;
                        }
                    }
                }

                if (processor == null)
                {
                    if (!processors.TryGetValue(cmd, out processor))
                        return null;

                    usedCommand = cmd;
                }

                if (string.IsNullOrEmpty(arguments))
                {
                    arguments = redeemer.Username;
                }

                if (processor.RequiresBroadcaster)
                {
                    return new RewardRedeemCommand(broadcaster, usedCommand, redeemer.Username);
                }
                else
                {
                    return new RewardRedeemCommand(redeemer, usedCommand, arguments);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}