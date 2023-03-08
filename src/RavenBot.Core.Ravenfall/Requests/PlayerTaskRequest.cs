using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall.Requests
{
    public class PlayerTaskRequest
    {
        public PlayerTaskRequest(User player, string task, string[] arguments)
        {
            Player = player;
            Task = task;
            Arguments = arguments;
        }

        public User Player { get; }
        public string Task { get; }
        public string[] Arguments { get; }
    }
}