using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenBot.Core.Handlers;
using RavenBot.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ROBot.Core.Chat.Discord
{
    public class DiscordFormatMessager
    {
        public async Task<bool> HandleCustomReplyAsync(
            ISocketMessageChannel channel,
            UserReference user,
            MessageReference reference,
            object[] args,
            string category,
            string[] tags)
        {
            // Process the chat message a final time before sending it off.
            if ((args == null) || (args.Length == 0 && (tags == null || tags.Length == 0) && string.IsNullOrEmpty(category)))
                return false;

            foreach (var arg in args)
            {
                if (arg is JToken token)
                {
                    // this might be a game object, we will only accept 1 game object
                    // per message so we can break out afterwards.

                    return await HandleCustomReplyAsync(channel, user, reference, token, category, tags);
                }
            }

            // in case we have category or tag we want to handle differently.
            //if (category == "duel" && tags.Length == 1 && tags[0] == "request")
            //{
            //    await channel.SendMessageAsync(
            //        "<@" + user.UserId + "> you have received a duel request from " + args[0] + "\n(Everyone will see this message, but only you can accept or decline)",
            //        components: new ComponentBuilder()
            //       .WithButton("Accept", "duel_accept", ButtonStyle.Primary)
            //       .WithButton("Decline", "duel_decline", ButtonStyle.Danger)
            //       .Build(),
            //        messageReference: reference);
            //    return true;
            //}

            return false;
        }

        private async Task<bool> HandleCustomReplyAsync(ISocketMessageChannel channel, UserReference user, MessageReference reference, JToken arg, string category, string[] tags)
        {
            //await channel.SendMessageAsync(message, messageReference: reference);

            if (TryDeserialize<PlayerInspect>(arg.ToString(), out var playerInspect))
            {
                return await SendPlayerStatsReplyAsync(channel, user, reference, playerInspect);
            }

            //try { }
            //    catch
            //  arg.ToString()

            return false;
        }

        private async Task<bool> SendPlayerStatsReplyAsync(ISocketMessageChannel channel, UserReference user, MessageReference reference, PlayerInspect inspect)
        {
            try
            {
                var builder = new EmbedBuilder();

                builder.Title = "Player stats for " + user.UserName;
                builder.Url = "https://www.ravenfall.stream/inspect/" + inspect.Id;

                switch (inspect.Location)
                {
                    case PlayerLocation.Ferry:
                        builder.Description = "You're currently sailing.";
                        break;
                    case PlayerLocation.War:
                        builder.Description = "You're currently in a **Stream Raid** on **" + inspect.Island + "**";
                        break;
                    case PlayerLocation.Raid:
                        builder.Description = "You're currently using **" + inspect.Training + "** in a **Raid** on **" + inspect.Island + "**";
                        break;
                    case PlayerLocation.Dungeon:
                        builder.Description = "You're currently using **" + inspect.Training + "** in a **Dungeon**";
                        break;
                    case PlayerLocation.Island:
                        builder.Description = "You're currently training **" + inspect.Training + "** on **" + inspect.Island + "**";
                        break;
                }

                if (inspect.Training != Skill.None)
                {
                    if (inspect.NextLevelUtc > DateTime.UtcNow)
                    {
                        var timeLeft = inspect.NextLevelUtc - DateTime.UtcNow;
                        builder = builder.AddField("Estimated time for next level", FormatTime(timeLeft));
                    }
                }

                builder.AddField("\u200B", "**__Stats__**", false);

                foreach (var skill in inspect.Skills)
                {
                    var p = Math.Floor(skill.Progress * 100);
                    var b = Math.Floor(skill.Bonus);
                    builder.AddField(skill.Name, "**" + skill.CurrentValue + "**/" + skill.Level
                        + " (" + p + "%)" +
                        (b > 0 ? " [+" + b + "]" : ""),
                    true);
                }
                var eq = inspect.EquipmentStats;

                builder.AddField("\u200B", "**__Equipment Stats__**", false);

                builder.AddField("Weapon Aim", eq.WeaponAim, true);
                builder.AddField("Weapon Power", eq.WeaponPower, true);

                builder.AddField("Magic Aim", eq.MagicAim, true);
                builder.AddField("Magic Power", eq.MagicPower, true);

                builder.AddField("Ranged Aim", eq.RangedAim, true);
                builder.AddField("Ranged Power", eq.RangedPower, true);

                builder.AddField("Armor Power", eq.ArmorPower, true);

                await channel.SendMessageAsync(
                    embed: builder.Build(),
                    messageReference: reference);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FormatTime(TimeSpan timeLeft)
        {
            if (timeLeft.Days >= 365 * 10_000)
            {
                return "```arm\nWhen hell freezes over\n```";
            }
            if (timeLeft.Days >= 365 * 1000)
            {
                return "```arm\nUnreasonably long\n```";
            }
            if (timeLeft.Days >= 365)
            {
                return "```arm\nWay too long\n```";
            }
            if (timeLeft.Days > 21)
            {
                return "```fix\n" + (int)(timeLeft.Days / 7) + " weeks\n```";
            }
            if (timeLeft.Days > 0)
            {
                return "```fix\n" + timeLeft.Days + " days, " + timeLeft.Hours + " hours\n```";
            }
            if (timeLeft.Hours > 0)
            {
                return "```json\n" + timeLeft.Hours + " hours, " + timeLeft.Minutes + " mins\n```";
            }
            if (timeLeft.Minutes > 0)
            {
                return "```json\n" + timeLeft.Minutes + " mins, " + timeLeft.Seconds + " secs\n```";
            }
            return "```json\n" + timeLeft.Seconds + " seconds\n```";
        }

        private bool TryDeserialize<T>(string json, out T value)
        {
            value = default;
            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
                return value != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserReference
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Platform { get; set; }
        public string UserName { get; set; }

        public UserReference() { }
        public UserReference(ICommandSender sender)
        {
            UserId = sender.UserId;
            Platform = sender.Platform;
            UserName = sender.Username;
        }

        public UserReference(GameMessageRecipent sender)
        {
            Id = sender.UserId;
            UserId = sender.PlatformId;
            Platform = sender.Platform;
            UserName = sender.PlatformUserName;
        }
    }
    public class PlayerInspect
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Rested { get; set; }
        public string Island { get; set; }
        public Skill Training { get; set; }
        public SkillInfo[] Skills { get; set; }
        public PlayerLocation Location { get; set; }
        public double ExpLeft { get; set; }
        public double ExpPerHour { get; set; }
        public DateTime NextLevelUtc { get; set; }
        public EquipmentStats EquipmentStats { get; set; }
    }

    public class SkillInfo
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public double Progress { get; set; }
        public int CurrentValue { get; set; }
        public int MaxLevel { get; set; }
        public float Bonus { get; set; }
    }

    public class EquipmentStats
    {
        public int ArmorPower { get; set; }
        public int WeaponAim { get; set; }
        public int WeaponPower { get; set; }
        public int MagicPower { get; set; }
        public int MagicAim { get; set; }
        public int RangedPower { get; set; }
        public int RangedAim { get; set; }

        public int ArmorPowerBonus { get; set; }
        public int WeaponAimBonus { get; set; }
        public int WeaponPowerBonus { get; set; }
        public int MagicPowerBonus { get; set; }
        public int MagicAimBonus { get; set; }
        public int RangedPowerBonus { get; set; }
        public int RangedAimBonus { get; set; }

        public int WeaponBonus => WeaponAimBonus + WeaponPowerBonus;
        public int RangedBonus => RangedAimBonus + RangedPowerBonus;
        public int MagicBonus => MagicAimBonus + MagicPowerBonus;
    }
    public enum PlayerLocation
    {
        Island,
        Ferry,
        Resting,
        Raid,
        Dungeon,
        War
    }

    public enum Skill
    {
        Attack,
        Defense,
        Strength,
        Health,
        Woodcutting,
        Fishing,
        Mining,
        Crafting,
        Cooking,
        Farming,
        Slayer,
        Magic,
        Ranged,
        Sailing,
        Healing,


        ///// <summary>
        ///// Refeers to training Attack Defense and Strength
        ///// </summary>
        //All = 100,

        None = 999,
    }
}