using System;

namespace RavenBot.Core.Templating
{
    public class StringTemplateProcessor : IStringTemplateProcessor
    {

        public string Process(IStringTemplate template, params object[] arguments)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var args = new IStringTemplateArgument[template.Parameters.Length];
            for (var i = 0; i < template.Parameters.Length; ++i)
            {
                var argValue = i < arguments.Length ? arguments[i] : null;
                args[i] = new StringTemplateArgument(template.Parameters[i], argValue);
            }

            return ProcessTemplateImpl(template, null, args);
        }

        public string Process(IStringTemplate template, params IStringTemplateArgument[] arguments)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            return ProcessTemplateImpl(template, null, arguments);
        }

        public string Process(IStringTemplate template, IStringTemplate templateOverride, params object[] arguments)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (templateOverride == null)
                throw new ArgumentNullException(nameof(templateOverride));

            var args = new IStringTemplateArgument[template.Parameters.Length];
            for (var i = 0; i < template.Parameters.Length; ++i)
            {
                var argValue = i < arguments.Length ? arguments[i] : null;
                args[i] = new StringTemplateArgument(template.Parameters[i], argValue);
            }

            return ProcessTemplateImpl(template, templateOverride, args);
        }

        private string ProcessTemplateImpl(IStringTemplate template, IStringTemplate templateOverride, IStringTemplateArgument[] arguments)
        {
            var output = FixMalformedTemplate(templateOverride?.Template ?? template.Template);
            foreach (var arg in arguments)
            {
                AssertArgumentType(arg);

                if (arg.BoxedValue != null)
                {
                    if (output.IndexOf("{" + arg.Parameter.Name + " ") >= 0)
                    {
                        output = output.Replace("{" + arg.Parameter.Name, arg.BoxedValue.ToString());
                    }
                    else
                    {
                        output = output.Replace("{" + arg.Parameter.Name + "}", arg.BoxedValue.ToString());
                    }
                }
            }

            return output.Replace("{", "").Replace("}", ""); // clear up malformed variables
        }

        private string FixMalformedTemplate(string templateData)
        {
            while (true)
            {
                var prevText = templateData;

                templateData = templateData
                    .Replace("{{", "{")
                    .Replace("}}", "}")
                    .Replace("  ", " ")
                    .Replace("{ ", "{")
                    .Replace(" }", "}")
                    .Replace("{}", "");

                if (prevText == templateData)
                    break;
            }

            return templateData;
        }

        private void AssertArgumentType(IStringTemplateArgument arg)
        {
            switch (arg.Parameter.ParameterType)
            {
                case TemplateVariableType.Unchecked:
                    return;

                case TemplateVariableType.Boolean:
                    if (arg.BoxedValue is bool) return;
                    break;
                case TemplateVariableType.Number:
                    if (arg.BoxedValue is int || arg.BoxedValue is uint || arg.BoxedValue is byte || arg.BoxedValue is sbyte ||
                        arg.BoxedValue is short || arg.BoxedValue is ushort || arg.BoxedValue is float || arg.BoxedValue is double ||
                        arg.BoxedValue is decimal || arg.BoxedValue is long || arg.BoxedValue is ulong) return;
                    break;
                case TemplateVariableType.String:
                    if (arg.BoxedValue is string) return;
                    break;
            }
            throw new InvalidCastException("Argument supplied to the template did not match the parameter type required. Argument Value: " + arg.BoxedValue + ", Expected Type: " + arg.Parameter.ParameterType);
        }
    }
}