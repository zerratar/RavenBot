using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;

namespace RavenBot.Core.Ravenfall
{
    public class RewardRedeemCommand : ICommand
    {
        private Player Player;

        public RewardRedeemCommand(Player player, string channel, string command, string arguments)
        {
            this.Player = player;
            this.Sender = new RewardRedeemSender(player);
            this.Arguments = arguments;
            this.Command = command;
            this.Channel = channel;
        }

        public ICommandSender Sender { get; }
        public string Channel { get; }
        public string Command { get; }
        public string Arguments { get; }

        internal class RewardRedeemSender : ICommandSender
        {
            private Player player;

            public RewardRedeemSender(Player player)
            {
                this.player = player;
            }

            public string UserId => player.UserId;

            public string Username => player.Username;

            public string DisplayName => player.DisplayName;

            public bool IsBroadcaster => player.IsBroadcaster;

            public bool IsModerator => player.IsModerator;

            public bool IsSubscriber => player.IsSubscriber;

            public bool IsVip => player.IsVip;

            public string ColorHex => player.Color;

            public bool IsVerifiedBot => false;

            public bool IsGameAdmin => false;

            public bool IsGameModerator => false;
        }
    }
}