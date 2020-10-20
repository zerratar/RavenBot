namespace RavenBot.Core
{
    public interface IStringTemplateParser
    {
        IStringTemplate Parse(string templateData, StringTemplateParserOption options = StringTemplateParserOption.PreserveTemplate);
    }
}