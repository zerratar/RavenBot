namespace RavenBot.Core.Templating
{
    public interface IStringTemplateArgument<T> : IStringTemplateArgument
    {
        T Value { get; }
    }
}