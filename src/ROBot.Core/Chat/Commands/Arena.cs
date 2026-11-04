using RavenBot.Core.Handlers;
using RavenBot.Core.Ravenfall;
using ROBot.Core.GameServer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Commands
{
    public class Arena : ChatCommandHandler
    {
        public override string Category => "PvP";
        public override string Description => "Fight other players in the arena on the island you are on. !arena on its own signs you up; the fight starts once at least two have joined. The streamer and moderators can start it early, call it off, or add and take out players.";
        public override string UsageExample => "!arena join";
        public override IReadOnlyList<ChatCommandInput> Inputs { get; } = new List<ChatCommandInput>
        {
            ChatCommandInput.Create("action", "join (or leave it out) signs you up, leave takes you out before the fight starts. For the streamer and moderators: start begins the fight without waiting for the countdown, end calls it off with no rewards, kick takes a player out and add signs one up.", "join", "leave", "start", "end", "kick", "add").NotRequired(),
            ChatCommandInput.Create("player", "For kick and add: the player.").NotRequired(),
        };

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
                    //var client = connection[cmd];

                    var command = cmd.Arguments?.Trim().ToLower();
                    if (string.IsNullOrEmpty(command) || command.Equals("join"))
                    {
                        await connection[cmd].JoinArenaAsync(player);
                    }
                    else if (command.Equals("leave"))
                    {
                        await connection[cmd].LeaveArenaAsync(player);
                    }
                    else if (command.Equals("start") || command.Equals("begin"))
                    {
                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                        {
                            await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_FORCE);
                            return;
                        }

                        await connection[cmd].StartArenaAsync(player);
                    }
                    else if (command.Equals("cancel") || command.Equals("end"))
                    {
                        if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                        {
                            await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_CANCEL);
                            return;
                        }

                        await connection[cmd].CancelArenaAsync(player);
                    }
                    else
                    {
                        if (command.StartsWith("kick "))
                        {
                            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                            {
                                await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_KICK);
                                return;
                            }
                            var targetPlayerName = command.Split(' ').LastOrDefault();
                            var targetPlayer = session.GetUserByName(targetPlayerName);
                            await connection[cmd].KickPlayerFromArenaAsync(player, targetPlayer);
                        }
                        else if (command.StartsWith("add "))
                        {
                            if (!cmd.Sender.IsBroadcaster && !cmd.Sender.IsModerator && !cmd.Sender.IsGameAdmin && !cmd.Sender.IsGameModerator)
                            {
                                await chat.SendReplyAsync(cmd, Localization.ARENA_PERM_ADD);
                                return;
                            }

                            // the named player; this was the sender, so !arena add <player> added the moderator
                            var targetPlayer = session.GetUserByName(command.Split(' ').LastOrDefault());
                            if (targetPlayer != null)
                                await connection[cmd].AddPlayerToArenaAsync(player, targetPlayer);
                        }
                    }

                }
            }
        }
    }
}
