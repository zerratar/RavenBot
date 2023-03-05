using System.Threading.Tasks;

namespace RavenBot.Core
{
    public interface IChatMessageFormatter
    {
        string Format(string message, params object[] args);
        string Format(string message, object[] args, StringTemplateParserOption options);
    }

    public interface IChatMessageTransformer
    {
        Task<string> TranslateAndPersonalizeAsync(string message, string language);
        Task<string> TranslateAsync(string message, string language);
        Task<string> PersonalizeAsync(string message);
    }
}
