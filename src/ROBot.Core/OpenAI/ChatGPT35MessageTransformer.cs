using Microsoft.Extensions.Logging;
using RavenBot.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ROBot.Core.OpenAI
{
    public class ChatGPT35MessageTransformer : IChatMessageTransformer
    {
        private readonly TimeSpan TransformationTimeout = TimeSpan.FromMinutes(1);
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly IAppSettings settings;
        private readonly OpenAI openAI;
        private DateTime noTransformationBefore;
        public ChatGPT35MessageTransformer(
            Microsoft.Extensions.Logging.ILogger logger,
            IAppSettings settings)
        {
            this.logger = logger;
            this.settings = settings;
            this.openAI = new OpenAI(new OpenAITokenString(settings.OpenAIAuthToken));
        }

        public async Task<string> PersonalizeAsync(string message)
        {
            try
            {
                if (DateTime.UtcNow < noTransformationBefore)
                {
                    return message;
                }

                var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "You have to make sure that the given sentences are gramatically correct and personal, also take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it."));
                if (completion == null) return message;
                return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
            }
            catch (System.Exception exc)
            {
                this.noTransformationBefore = DateTime.UtcNow.Add(TransformationTimeout);
                logger.LogError("PersonalizeAsync failed for message '" + message + "' Error: " + exc.Message + ", no new messages will be transformed for " + TransformationTimeout.TotalSeconds + " seconds.");
                return message;
            }
        }

        public async Task<string> TranslateAndPersonalizeAsync(string message, string language)
        {
            try
            {
                if (DateTime.UtcNow < noTransformationBefore)
                {
                    return message;
                }

                if (string.IsNullOrEmpty(language) || language.StartsWith("none", System.StringComparison.OrdinalIgnoreCase))
                {
                    return await PersonalizeAsync(message);
                }

                var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "You have to make sure that the given sentences are gramatically correct and personal, also take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it.And make sure its translated into " + language + "."));
                if (completion == null) return message;
                return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
            }
            catch (System.Exception exc)
            {
                this.noTransformationBefore = DateTime.UtcNow.Add(TransformationTimeout);
                logger.LogError("TranslateAndPersonalizeAsync failed for message '" + message + "' into langauge '" + language + "'. Error: " + exc.Message + ", no new messages will be transformed for " + TransformationTimeout.TotalSeconds + " seconds.");
                return message;
            }
        }

        public async Task<string> TranslateAsync(string message, string language)
        {
            try
            {
                if (DateTime.UtcNow < noTransformationBefore)
                {
                    return message;
                }

                if (string.IsNullOrEmpty(language) || language.StartsWith("none", System.StringComparison.OrdinalIgnoreCase))
                {
                    return message;
                }

                var completion = await openAI.GetCompletionAsync(message, ChatMessage.Create("system", "Translate the given sentences into " + language + ", take into consideration that any words wrapped with { } are variables and will be changed afterwards so do not remove those. Reply with only the improved result and nothing else. Do not wrap the result in \" even if the input has it."));
                if (completion == null) return message;
                return completion.Choices.FirstOrDefault()?.Message.Content ?? message;
            }
            catch (System.Exception exc)
            {
                this.noTransformationBefore = DateTime.UtcNow.Add(TransformationTimeout);
                logger.LogError("TranslateAsync failed for message '" + message + "' into langauge '" + language + "'. Error: " + exc.Message + ", no new messages will be transformed for " + TransformationTimeout.TotalSeconds + " seconds.");
                return message;
            }
        }
    }
}
