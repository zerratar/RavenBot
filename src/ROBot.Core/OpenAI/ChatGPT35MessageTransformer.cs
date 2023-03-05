using RavenBot.Core;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.OpenAI
{
    public class ChatGPT35MessageTransformer : IChatMessageTransformer
    {
        private readonly ROBot.Core.IAppSettings settings;
        private readonly OpenAI openAI;

        public ChatGPT35MessageTransformer(ROBot.Core.IAppSettings settings)
        {
            this.settings = settings;
            this.openAI = new OpenAI(new OpenAITokenString(settings.OpenAIAuthToken));
        }

        public async Task<string> PersonalizeAsync(string message)
        {
            var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "You have to make sure that the given sentences are gramatically correct and personal, also take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it."));
            if (completion == null) return message;
            return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
        }

        public async Task<string> TranslateAndPersonalizeAsync(string message, string language)
        {
            var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "You have to make sure that the given sentences are gramatically correct and personal, also take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it.And make sure its translated into " + language + "."));
            if (completion == null) return message;
            return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
        }

        public async Task<string> TranslateAsync(string message, string language)
        {
            var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "Translate the given sentences into " + language + ", take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it."));
            if (completion == null) return message;
            return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
        }
    }
}
