namespace RavenBot.Core.Templating
{
    public class StringTemplateArgument : IStringTemplateArgument
    {
        public StringTemplateArgument(IStringTemplateParameter parameter, object value)
        {
            Parameter = parameter;
            BoxedValue = value;
        }

        public IStringTemplateParameter Parameter { get; }

        public object BoxedValue { get; }
    }
}