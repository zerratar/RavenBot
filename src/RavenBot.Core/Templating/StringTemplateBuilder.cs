using System.Collections.Generic;
using System.Linq;

namespace RavenBot.Core.Templating
{
    public class StringTemplateBuilder : IStringTemplateBuilder
    {
        private string templateData;
        private List<IStringTemplateParameter> templateParams = new List<IStringTemplateParameter>();

        public IStringTemplate Build()
        {
            try
            {
                return new StringTemplate
                {
                    Parameters = templateParams.ToArray(),
                    Template = templateData
                };
            }
            finally
            {
                templateData = null;
                templateParams = new List<IStringTemplateParameter>();
            }
        }

        public IStringTemplateBuilder SetFormat(string format)
        {
            templateData = format;
            return this;
        }

        public IStringTemplateBuilder AddParameter(string parameterName, TemplateVariableType parameterType = TemplateVariableType.Unchecked)
        {
            templateParams.Add(new StringTemplateParameter
            {
                ParameterType = parameterType,
                Name = parameterName
            });
            return this;
        }

        private class StringTemplateParameter : IStringTemplateParameter
        {
            public TemplateVariableType ParameterType { get; set; }
            public string Name { get; set; }
        }


        private class StringTemplate : IStringTemplate
        {
            public IStringTemplateParameter[] Parameters { get; set; }
            public string Template { get; set; }

            public IStringTemplateArgument<T> CreateArgument<T>(int argIndex, T value)
            {
                return new StringTemplateArgument<T>(Parameters[argIndex], value);
            }

            public IStringTemplateArgument<T> CreateArgument<T>(string parameterName, T value)
            {
                return new StringTemplateArgument<T>(Parameters.FirstOrDefault(x => x.Name == parameterName), value);
            }

            public IStringTemplateArgument<T> CreateArgument<T>(IStringTemplateParameter parameter, T value)
            {
                return new StringTemplateArgument<T>(parameter, value);
            }
        }
    }
}