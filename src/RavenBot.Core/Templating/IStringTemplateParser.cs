namespace RavenBot.Core.Templating
{
    public interface IStringTemplateParser
    {
        IStringTemplate Parse(string templateData, StringTemplateParserOption options = StringTemplateParserOption.PreserveTemplate);
    }
}