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
        public void TestTwitchMessageFormatter_ManipulatedFormat_FormatMessage()
        {
            var testFormat = "Test {a} {b}";
            var newFormat = "HEHE {b} {a} {b}";

            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());

            formatter.OverrideFormat(testFormat, newFormat);

            var result = formatter.Format(testFormat, "kaaru", "mantis");
            Assert.AreEqual("HEHE mantis kaaru mantis", result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_TooManyArgs_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format("Hello world {a}", "kaaru", "mantis");
            Assert.AreEqual("Hello world kaaru", result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_NotEnoughArgs_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format("Hello world {a} {b}", "kaaru");
            Assert.AreEqual("Hello world kaaru b", result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_NotEnoughArgs2_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format("Hello world {a} {b}", "kaaru", "");
            Assert.AreEqual("Hello world kaaru ", result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_MixArgs_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format("Hello world {a} {b} {a}", "kaaru", "mantis");
            Assert.AreEqual("Hello world kaaru mantis kaaru", result);
        }


        [TestMethod]
        public void TestTwitchMessageFormatter_MessyData2_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format(messyTemplateData2, templateVarValue);
            Assert.AreEqual(expected_result, result);
        }

        [TestMethod]
        public void TestTwitchMessageFormatter_MessyData_FormatMessage()
        {
            var formatter = new TwitchMessageFormatter(new StringProvider(), new StringTemplateProcessor(), new StringTemplateParser());
            var result = formatter.Format(messyTemplateData, templateVarValue);
            Assert.AreEqual(expected_result, result);
        }

        [TestMethod]
        public void TestTemplateParser_MessyData_ReturnFixedTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(messyTemplateData, StringTemplateParserOption.FixMalformedTemplate);

            Assert.AreEqual(templateData, template.Template);
            Assert.AreEqual(1, template.Parameters.Length);
            Assert.AreEqual(templateVarName, template.Parameters[0].Name);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(expected_result, a);
        }

        [TestMethod]
        public void TestTemplateParser_MessyData_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(messyTemplateData);

            Assert.AreEqual(messyTemplateData, template.Template);
            Assert.AreEqual(1, template.Parameters.Length);
            Assert.AreEqual(templateVarName, template.Parameters[0].Name);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(expected_result, a);
        }

        [TestMethod]
        public void TestTemplateParser_ExtraWhitespaces_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(templateDataWhitespaces);

            Assert.AreEqual(templateDataWhitespaces, template.Template);
            Assert.AreEqual(1, template.Parameters.Length);
            Assert.AreEqual(templateVarName, template.Parameters[0].Name);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(expected_result, a);
        }

        [TestMethod]
        public void TestTemplateParser_ReturnValidTemplate()
        {
            IStringTemplateParser parser = new StringTemplateParser();
            IStringTemplate template = parser.Parse(templateData);

            Assert.AreEqual(templateData, template.Template);
            Assert.AreEqual(1, template.Parameters.Length);
            Assert.AreEqual(templateVarName, template.Parameters[0].Name);

            IStringTemplateProcessor processor = new StringTemplateProcessor();
            var a = processor.Process(template, templateVarValue);

            Assert.AreEqual(expected_result, a);
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

            Assert.AreEqual(expected_result, a);
            Assert.AreEqual(expected_result, b);
            Assert.AreEqual(expected_result, c);
            Assert.AreEqual(expected_result, d);
        }
    }
}
