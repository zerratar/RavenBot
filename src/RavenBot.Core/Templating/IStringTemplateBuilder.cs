namespace RavenBot.Core
{
    public interface IStringTemplateBuilder
    {
        IStringTemplateBuilder SetFormat(string format);
        IStringTemplateBuilder AddParameter(string parameterName, TemplateVariableType parameterType = TemplateVariableType.Unchecked);
        IStringTemplate Build();
    }
}