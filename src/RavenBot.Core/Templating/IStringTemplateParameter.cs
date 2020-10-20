namespace RavenBot.Core
{
    public interface IStringTemplateParameter
    {
        TemplateVariableType ParameterType { get; }
        string Name { get; }
    }
}