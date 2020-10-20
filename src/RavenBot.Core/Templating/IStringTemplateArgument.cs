namespace RavenBot.Core
{
    public interface IStringTemplateArgument
    {
        IStringTemplateParameter Parameter { get; }
        object BoxedValue { get; }
    }
}