namespace RavenBot.Core.Handlers
{

    public interface ICommand
    {
        ICommandSender Sender { get; }
        ICommandChannel Channel { get; }
        string Command { get; }
        string Arguments { get; }
        /// <summary>
        ///     A message correlation id, most of the cases this will be the message id that first issued a command that was sent to the game and will be used for direct reply to.
        ///     This will be empty if the client sent a message related to an event or if the user used the Twitcch Extension or 3rd party app to send a command.
        /// </summary>
        string CorrelationId { get; }
        /// <summary>
        ///     While correlation id should always be used to refeer to a recipent or target, most commonly it will be the message id to reply to. 
        ///     But when that is missing and we still want to mention a user, this is the username to use.
        /// </summary>
        string Mention { get; }
    }

    public interface ICommandChannel
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
    }

    public interface ICommandSender
    {
        string UserId { get; }
        string Platform { get; }
        string Username { get; }
        string DisplayName { get; }
        bool IsGameAdmin { get; }
        bool IsGameModerator { get; }
        bool IsBroadcaster { get; }
        bool IsModerator { get; }
        bool IsSubscriber { get; }
        bool IsVip { get; }
        bool IsVerifiedBot { get; }
        string ColorHex { get; }
    }
}