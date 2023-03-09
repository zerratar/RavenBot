namespace RavenBot.Core.Templating
{
    public interface IStringTemplateArgument
    {
        IStringTemplateParameter Parameter { get; }
        object BoxedValue { get; }
    }
}