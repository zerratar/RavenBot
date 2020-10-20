namespace RavenBot.Core.Twitch
{
    public class TwitchMessageFormatter : ITwitchMessageFormatter
    {
        private readonly IStringProvider stringProvider;
        private readonly IStringTemplateProcessor processor;
        private readonly IStringTemplateParser parser;

        public TwitchMessageFormatter(
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
            message = stringProvider.Get(message);
            var template = parser.Parse(message, options);
            return processor.Process(template, args);
        }
    }
}
