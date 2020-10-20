using Microsoft.VisualStudio.TestTools.UnitTesting;
using RavenBot.Core;
using RavenBot.Core.Twitch;

namespace RavenBot.Tests
{
    [TestClass]
    public class StringTemplateTests
    {
        const string messyTemplateData = "A level {{level} raid boss {}}{ }appeared! Type !raid to join!";
        const string messyTemplateData2 = "A level {{level raid boss {}}{ }{appeared! Type !raid to join!";
        const string templateData = "A level {level} raid boss appeared! Type !raid to join!";
        const string templateDataWhitespaces = "A level { level } raid boss appeared! Type !raid to join!";

        const string templateVarName = "level";
        const int templateVarValue = 1234;

        const string expected_result = "A level 1234 raid boss appeared! Type !raid to join!";


        [TestMethod]
        public void TestTwitchMessageFormatter_MessyData2_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format(messyTemplateData2, templateVarValue);
            Assert.AreEqual(result, expected_result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_MessyData_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format(messyTemplateData, templateVarValue);
            Assert.AreEqual(result, expected_result);
        }

        [TestMethod]
        public void TestTemplateParser_MessyData_ReturnFixedTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(messyTemplateData, StringTemplateParserOption.FixMalformedTemplate);

            Assert.AreEqual(template.Template, templateData);
            Assert.AreEqual(template.Parameters.Length, 1);
            Assert.AreEqual(template.Parameters[0].Name, templateVarName);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(a, expected_result);
        }

        [TestMethod]
        public void TestTemplateParser_MessyData_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(messyTemplateData);

            Assert.AreEqual(template.Template, messyTemplateData);
            Assert.AreEqual(template.Parameters.Length, 1);
            Assert.AreEqual(template.Parameters[0].Name, templateVarName);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(a, expected_result);
        }

        [TestMethod]
        public void TestTemplateParser_ExtraWhitespaces_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(templateDataWhitespaces);

            Assert.AreEqual(template.Template, templateDataWhitespaces);
            Assert.AreEqual(template.Parameters.Length, 1);
            Assert.AreEqual(template.Parameters[0].Name, templateVarName);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(a, expected_result);
        }

        [TestMethod]
        public void TestTemplateParser_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(templateData);

            Assert.AreEqual(template.Template, templateData);
            Assert.AreEqual(template.Parameters.Length, 1);
            Assert.AreEqual(template.Parameters[0].Name, templateVarName);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(a, expected_result);
        }

        [TestMethod]
        public void TestTemplateBuilder_ReturnValidTemplate()
        {
            IStringTemplateBuilder builder = new StringTemplateBuilder();
            IStringTemplate template = builder
                .SetFormat(templateData)
                .AddParameter(templateVarName)
                .Build();

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            var argB = template.CreateArgument(0, templateVarValue);
            var b = processor.Process(template, argB);

            var argC = template.CreateArgument(templateVarName, templateVarValue);
            var c = processor.Process(template, argC);

            var argD = template.CreateArgument(template.Parameters[0], templateVarValue);
            var d = processor.Process(template, argD);

            Assert.AreEqual(a, expected_result);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a, c);
            Assert.AreEqual(a, d);
        }
    }
}
