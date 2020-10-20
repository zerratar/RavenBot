using System;

namespace RavenBot.Core
{
    public enum StringTemplateParserOption
    {
        PreserveTemplate,
        FixMalformedTemplate
    }

    public class StringTemplateParser : IStringTemplateParser
    {
        private static readonly char[] validIdentifierTokens = "qwertyuiopåasdfghjklöäzxcvbnm1234567890".ToCharArray();

        public IStringTemplate Parse(string templateData, StringTemplateParserOption options = StringTemplateParserOption.PreserveTemplate)
        {
            var builder = new StringTemplateBuilder();

            AddParameters(builder, templateData);

            if (options == StringTemplateParserOption.FixMalformedTemplate)
                templateData = FixMalformedTemplate(templateData);

            builder.SetFormat(templateData);
            return builder.Build();
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

        private void AddParameters(StringTemplateBuilder builder, string templateData)
        {
            var tokenIndex = 0;
            while (tokenIndex < templateData.Length)
            {
                var token = templateData[tokenIndex];
                var parameter = ParseToken(token, templateData, ref tokenIndex);
                if (parameter != null)
                {
                    builder.AddParameter(parameter);
                }
            }
        }

        private string ParseToken(char token, string tokens, ref int tokenIndex)
        {
            switch (token)
            {
                case '{':
                    return ParseParameter(tokens, ref tokenIndex);
            }

            ++tokenIndex;
            return null;
        }

        private string ParseParameter(string tokens, ref int tokenIndex)
        {
            var token = tokens[++tokenIndex];
            if (char.IsWhiteSpace(token))
                return ParseParameter(tokens, ref tokenIndex);

            if (!IsValidIdentifier(token))
                return null;

            var parameter = "";
            while (IsValidIdentifier(token))
            {
                parameter += token;
                token = tokens[++tokenIndex];
            }

            if (string.IsNullOrWhiteSpace(parameter))
                return null;

            return parameter;
        }

        private bool IsValidIdentifier(char token)
        {
            return Array.IndexOf(validIdentifierTokens, char.ToLower(token)) >= 0;
        }
    }
}