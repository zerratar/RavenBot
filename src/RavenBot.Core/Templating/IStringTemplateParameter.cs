namespace RavenBot.Core.Templating
{
    public interface IStringTemplateParameter
    {
        TemplateVariableType ParameterType { get; }
        string Name { get; }
    }
}