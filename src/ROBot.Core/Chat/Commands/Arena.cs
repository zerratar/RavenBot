using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Arena : ChatCommandHandler
    {
        public override async Task HandleAsync(IBotServer game, IChatCommandClient chat, ICommand cmd)
        {
            var channel = cmd.Channel;
            var session = game.GetSession(channel);
            if (session != null)
            {
                var connection = game.GetConnection(session);
                if (connection != null)
                {
                    var player = session.Get(cmd);

                    var command = cmd.Arguments?.Trim().ToLower();
                    if (string.IsNullOrEmpty(command) || command.Equals("join"))
                    {
                        await connection.Reply(cmd.CorrelationId).JoinArenaAsync(player);
                    }
                    else if (command.Equals("leave"))
                    {
                        await connection.Reply(cmd.CorrelationId).LeaveArenaAsync(player);
                    }
                    else if (command.Equals("start") || command.Equals("begin"))
                    {
                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                        {
                            chat.SendReply(cmd, Localization.ARENA_PERM_FORCE);
                            return;
                        }

                        await connection.Reply(cmd.CorrelationId).StartArenaAsync(player);
                    }
                    else if (command.Equals("cancel") || command.Equals("end"))
                    {
                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                        {
                            chat.SendReply(cmd, Localization.ARENA_PERM_CANCEL);
                            return;
                        }

                        await connection.Reply(cmd.CorrelationId).CancelArenaAsync(player);
                    }
                    else
                    {
                        if (command.StartsWith("kick "))
                        {
                            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                            {
                                chat.SendReply(cmd, Localization.ARENA_PERM_KICK);
                                return;
                            }
                            var targetPlayerName = command.Split(' ').LastOrDefault();
                            var targetPlayer = session.GetUserByName(targetPlayerName);
                            await connection.Reply(cmd.CorrelationId).KickPlayerFromArenaAsync(player, targetPlayer);
                        }
                        else if (command.StartsWith("add "))
                        {
                            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                            {
                                chat.SendReply(cmd, Localization.ARENA_PERM_ADD);
                                return;
                            }

                            var targetPlayer = session.Get(cmd);
                            await connection.Reply(cmd.CorrelationId).AddPlayerToArenaAsync(player, targetPlayer);
                        }
                    }

                }
            }
        }
    }
}
