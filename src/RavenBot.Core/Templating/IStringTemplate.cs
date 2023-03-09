namespace RavenBot.Core.Templating
{

    public interface IStringTemplate
    {
        IStringTemplateParameter[] Parameters { get; }
        string Template { get; }

        IStringTemplateArgument<T> CreateArgument<T>(int argIndex, T value);
        IStringTemplateArgument<T> CreateArgument<T>(string parameterName, T value);
        IStringTemplateArgument<T> CreateArgument<T>(IStringTemplateParameter parameter, T value);
    }
}