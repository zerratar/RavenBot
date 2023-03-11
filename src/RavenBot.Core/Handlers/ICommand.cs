namespace RavenBot.Core.Handlers
{

    public interface ICommand
    {
        ICommandSender Sender { get; }
        ICommandChannel Channel { get; }
        string Command { get; }
        string Arguments { get; }
        string CorrelationId { get; }
    }

    public interface ICommandChannel
    {
        public ulong Id { get; }
        public string Name { get; }
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