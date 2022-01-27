﻿using RavenBot.Core.Extensions;
using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall.Models;
using System.Text;

namespace RavenBot.Core.Ravenfall
{
    public class RewardRedeemCommand : ICommand
    {
        private Player Player;

        public RewardRedeemCommand(Player player, string channel, string command, string arguments)
        {
            this.Player = player;
            this.Sender = new RewardRedeemSender(player);
            this.Arguments = arguments.AsUTF8();
            this.Command = command.AsUTF8();
            this.Channel = channel;
        }

        public ICommandSender Sender { get; }
        public string Channel { get; }
        public string Command { get; }
        public string Arguments { get; }

        public override string ToString()
        {
            return (Player?.Username ?? Sender?.Username ?? "???") + ": #" + Channel + ", " + Command + " " + Arguments;
        }

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