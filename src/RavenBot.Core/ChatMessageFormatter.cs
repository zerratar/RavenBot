using RavenBot.Core.Templating;
using System;

namespace RavenBot.Core
{
    public class ChatMessageFormatter : IChatMessageFormatter
    {
        private readonly IStringProvider stringProvider;
        private readonly IStringTemplateProcessor processor;
        private readonly IStringTemplateParser parser;

        public ChatMessageFormatter(
            IStringProvider stringProvider,
            IStringTemplateProcessor processor,
            IStringTemplateParser parser)
        {
            this.stringProvider = stringProvider;
            this.processor = processor;
            this.parser = parser;
        }

        public string Format(string message, params object[] args)
        {
            return Format(message, args, StringTemplateParserOption.FixMalformedTemplate);
        }

        public string Format(string message, object[] args, StringTemplateParserOption options)
        {
            if (!message.Contains('{'))
            {
                return message;
            }

            var template = parser.Parse(message, options);

            var str = stringProvider.Get(message);
            if (str == message)
            {
                return processor.Process(template, args);
            }

            var templateOverride = parser.Parse(str, options);
            return processor.Process(template, templateOverride, args);
        }

        public void OverrideFormat(string oldValue, string newValue)
        {
            stringProvider.Override(oldValue, newValue);
        }
    }
}
