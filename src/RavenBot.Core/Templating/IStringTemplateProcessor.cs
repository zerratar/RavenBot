namespace RavenBot.Core
{
    public interface IStringTemplateProcessor
    {
        string Process(IStringTemplate template, params object[] arguments);
        string Process(IStringTemplate template, params IStringTemplateArgument[] arguments);
        string Process(IStringTemplate originalTemplate, IStringTemplate templateOverride, params object[] arguments);
    }
}