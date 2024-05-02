namespace RavenBot.Core.Ravenfall
{
    public static class Settings
    {
        public const int UNITY_SERVER_PORT = 4040;
    }

    public static class Localization
    {
        public const string PERMISSION_DENIED = "You do not have permissions to use this command.";
        public const string GAME_NOT_STARTED = "Ravenfall has not started.";

        public const string HELP = "The commands are available in the panels below the stream :-) Too many commands.";

        public const string TASK_NO_ARG = "You need to specify a task, currently supported tasks: {tasks}";
        public const string TRAIN_NO_ARG = "You need to specify a skill to train, currently supported skills: {skills} or !sail for sailing.";
        public const string REDEEM_NO_ARG = "You need to specify what to redeem, like: item, exp. See the options available in the Tavern.";
        public const string TOGGLE_NO_ARG = "You need to specify what to toggle, like: helm or pet";

        public const string MARKET_TRADE_INVALID_ACTION = "Invalid market action, must be 'buy', 'sell' or 'value'.";
        public const string MARKET_TRADE_NO_ARG = "{command} <action> <item> (optional: <amount>, default 1) <price per item>";
        public const string OLD_TRADE_NO_ARG = "{command} <item> (optional: <amount>, default 1) <price per item>";

        public const string TRAVEL_NO_ARG = "You must specify a destination, !travel <destination>";
        public const string VALUE_NO_ARG = "{command} <item>";
        public const string VENDOR_NO_ARG = "{command} <item> (optional: <amount>, default 1)";
        public const string DUEL_NO_ARG = "To duel a player you need to specify their name. ex: '!duel JohnDoe', to accept or decline a duel request use '!duel accept' or '!duel decline'. You may also cancel an ongoing request by using '!duel cancel'";

        public const string TRAIN_INVALID = "You cannot train '{skill}'.";
        public const string TOGGLE_INVALID = "{invalidOption} cannot be toggled.";

        public const string COMMAND_COOLDOWN = "You must wait another {secondsLeft} secs to use that command.";

        public const string GIFT_HELP = "{command} <playername> <item> (optional: <amount>, default 1)";
        public const string CRAFT_HELP = "{command} <item>";

        public const string OBSERVE_PERM = "You do not have permission to set the currently observed player.";

        public const string KICK_PERM = "You do not have permission to kick a player from the game.";
        public const string KICK_NO_USER = "You are kicking who? Provide a username";

        public const string ARENA_PERM_ADD = "You do not have permission to add a player to the arena.";
        public const string ARENA_PERM_KICK = "You do not have permission to kick a player from the arena.";
        public const string ARENA_PERM_CANCEL = "You do not have permission to cancel the arena.";
        public const string ARENA_PERM_FORCE = "You do not have permission to force start the arena.";

        public static class Twitch
        {
            public const string THANK_YOU_BITS = "Thank you {displayName} for the {bits} bits!!! <3";
            public const string THANK_YOU_SUB = "Thank you {displayName} for the sub!!! <3";
            public const string THANK_YOU_RESUB = "Thank you {displayName} for the resub!!! <3";
            public const string THANK_YOU_GIFT_SUB = "Thank you {displayName} for the gifted sub!!! <3";
            public const string THANK_YOU_RAID = "Thank you {displayName} for the raid! <3";
        }
    }
}