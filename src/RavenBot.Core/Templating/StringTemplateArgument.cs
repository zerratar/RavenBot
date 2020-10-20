namespace RavenBot.Core
{
    public class StringTemplateArgument<T> : IStringTemplateArgument<T>
    {
        public StringTemplateArgument(IStringTemplateParameter parameter, T value)
        {
            Parameter = parameter;
            Value = value;
        }

        public T Value { get; }

        public IStringTemplateParameter Parameter { get; }

        public object BoxedValue => Value;
    }
}