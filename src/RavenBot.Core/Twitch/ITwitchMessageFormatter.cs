namespace RavenBot.Core.Twitch
{
    public interface ITwitchMessageFormatter
    {
        string Format(string message, params object[] args);
        string Format(string message, object[] args, StringTemplateParserOption options);
    }
}
