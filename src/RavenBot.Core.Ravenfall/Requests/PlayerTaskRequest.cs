using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerTaskRequest
    {
        public PlayerTaskRequest(Player player, string task, string[] arguments)
        {
            Player = player;
            Task = task;
            Arguments = arguments;
        }

        public Player Player { get; }
        public string Task { get; }
        public string[] Arguments { get; }
    }
}