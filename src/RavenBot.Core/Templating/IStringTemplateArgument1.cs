namespace RavenBot.Core
{
    public interface IStringTemplateArgument<T> : IStringTemplateArgument
    {
        T Value { get; }
    }
}