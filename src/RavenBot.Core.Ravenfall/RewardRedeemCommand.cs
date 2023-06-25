using RavenBot.Core.Chat.Twitch;
using RavenBot.Core.Extensions;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System.Text;

namespace RavenBot.Core.Ravenfall
{
    public class RewardRedeemCommand : ICommand
    {
        private User Player;

        public RewardRedeemCommand(User player, string channel, string command, string arguments)
        {
            this.Player = player;
            this.Sender = new RewardRedeemSender(player);
            this.Arguments = arguments.AsUTF8();
            this.Command = command.AsUTF8();
            this.Channel = new TwitchCommand.TwitchChannel(channel);
            this.CorrelationId = player.PlatformId;
            this.Mention = "@" + player.Username;
        }

        public ICommandSender Sender { get; }
        public ICommandChannel Channel { get; }
        public string Command { get; }
        public string Arguments { get; }
        public string CorrelationId { get; set; }
        public string Mention { get; set; }
        public override string ToString()
        {
            return (Player?.Username ?? Sender?.Username ?? "???") + ": #" + Channel + ", " + Command + " " + Arguments;
        }

        internal class RewardRedeemSender : ICommandSender
        {
            private User player;

            public RewardRedeemSender(User player)
            {
                this.player = player;
            }
            public string Platform => player.Platform;
            public string UserId => player.PlatformId;

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