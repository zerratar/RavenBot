using Microsoft.IdentityModel.Tokens;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using RavenBot.Core.Ravenfall.Models;
using System.Linq;

namespace RavenBot.Core.Ravenfall
{
    public class CommandProvider : ICommandProvider
    {
        private readonly IUserProvider playerProvider;
        private readonly ICommandHandler commandHandler;

        public CommandProvider(
            IUserProvider playerProvider,
            ICommandHandler commandHandler)
        {
            this.playerProvider = playerProvider;
            this.commandHandler = commandHandler;
        }

        public ICommand GetCommand(User redeemer, string channel, string command, string arguments)
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
                    return new RewardRedeemCommand(broadcaster, channel, usedCommand, redeemer.Username);
                }
                else
                {
                    return new RewardRedeemCommand(redeemer, channel, usedCommand, arguments);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}