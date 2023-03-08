using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public interface ICommandProvider
    {
        ICommand GetCommand(User redeemer, string channel, string command, string arguments);
    }
}