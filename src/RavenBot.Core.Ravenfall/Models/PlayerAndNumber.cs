namespace RavenBot.Core.Ravenfall.Models
{
    public class PlayerAndNumber
    {
        public PlayerAndNumber(User player, int number)
        {
            Player = player;
            Number = number;
        }

        public User Player { get; }
        public int Number { get; }
    }
}
