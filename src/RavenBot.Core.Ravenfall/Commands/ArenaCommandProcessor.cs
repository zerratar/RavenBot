using System.Linq;
using System.Threading.Tasks;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;

namespace RavenBot.Core.Ravenfall.Commands
{
    public class ArenaCommandProcessor : CommandProcessor
    {
        private readonly IRavenfallClient game;
        private readonly IPlayerProvider playerProvider;

        public ArenaCommandProcessor(IRavenfallClient game, IPlayerProvider playerProvider)
        {
            this.game = game;
            this.playerProvider = playerProvider;
        }

        public override async Task ProcessAsync(IMessageBroadcaster broadcaster, ICommand cmd)
        {
            if (!await this.game.ProcessAsync(Settings.UNITY_SERVER_PORT))
            {
                broadcaster.Broadcast(Localization.GAME_NOT_STARTED);
                return;
            }

            // --- PLayers only ---
            //!arena leave
            //!arena, !arena join

            // --- Broadcaster Only ---
            //!arena start,begin
            //!arena cancel,cancel
            //!arena kick <player>
            //!arena add <player>

            var command = cmd.Arguments?.Trim().ToLower();
            var player = playerProvider.Get(cmd.Sender);
            if (string.IsNullOrEmpty(command) || command.Equals("join"))
            {
                await game.SendPlayerJoinArenaAsync(player);
            }
            else if (command.Equals("leave"))
            {
                await game.SendPlayerLeaveArenaAsync(player);
            }
            else if (command.Equals("start") || command.Equals("begin"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                {
                    broadcaster.Send(cmd.Sender.Username, "You do not have permission to force start the arena.");
                    //broadcaster.Broadcast("You do not have permission to force start the arena.");
                    return;
                }

                await game.SendStartArenaAsync(player);
            }
            else if (command.Equals("cancel") || command.Equals("end"))
            {
                if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                {
                    //broadcaster.Broadcast(
                    broadcaster.Send(cmd.Sender.Username,
                    "You do not have permission to cancel the arena.");
                    return;
                }

                await game.SendCancelArenaAsync(player);
            }
            else
            {
                var targetPlayerName = command.Split(' ').LastOrDefault();
                if (command.StartsWith("kick "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                    {
                        //broadcaster.Broadcast(
                        broadcaster.Send(cmd.Sender.Username,
                            "You do not have permission to kick a player from the arena.");
                        return;
                    }

                    var targetPlayer = playerProvider.Get(cmd.Sender);
                    await game.SendKickPlayerFromArenaAsync(player, targetPlayer);
                }
                else if (command.StartsWith("add "))
                {
                    if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator)
                    {
                        //broadcaster.Broadcast(
                        broadcaster.Send(cmd.Sender.Username,
                        "You do not have permission to add a player to the arena.");
                        return;
                    }

                    var targetPlayer = playerProvider.Get(cmd.Sender);
                    await game.SendAddPlayerToArenaAsync(player, targetPlayer);
                }
            }
        }
    }
}