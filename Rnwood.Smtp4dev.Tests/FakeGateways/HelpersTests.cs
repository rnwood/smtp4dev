using System;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class PhoneNormalizerTests
    {
        [Theory]
        [InlineData("+420700000001", "+420700000001")]
        [InlineData("00420700000001", "+420700000001")]
        [InlineData(" +420 700 000 001 ", "+420700000001")]
        [InlineData("00 420 700 000 001", "+420700000001")]
        [InlineData("700000001", "700000001")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Normalize(string input, string expected)
        {
            PhoneNormalizer.Normalize(input).Should().Be(expected);
        }
    }

    public class CodeExtractorTests
    {
        [Fact]
        public void ReturnsGroup1WhenPresent()
        {
            CodeExtractor.Extract(CodeExtractor.Compile(@"kod: (\d{6})"), "pouzijte kod: 123456.").Should().Be("123456");
        }

        [Fact]
        public void ReturnsWholeMatchWithoutGroup()
        {
            CodeExtractor.Extract(CodeExtractor.Compile(@"\d{6}"), "pouzijte kod: 123456.").Should().Be("123456");
        }

        [Fact]
        public void ReturnsNullWhenNoMatchOrNoText()
        {
            CodeExtractor.Extract(CodeExtractor.Compile(@"\d{6}"), "no digits").Should().BeNull();
            CodeExtractor.Extract(CodeExtractor.Compile(@"\d{6}"), null).Should().BeNull();
        }

        [Fact]
        public void InvalidRegex_Throws()
        {
            Action act = () => CodeExtractor.Compile("(");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CatastrophicRegex_ThrowsTimeout()
        {
            var regex = CodeExtractor.Compile(@"^(a+)+$");
            Action act = () => CodeExtractor.Extract(regex, new string('a', 60) + "!");
            act.Should().Throw<RegexMatchTimeoutException>();
        }

        [Fact]
        public void RegexHasOneSecondTimeout()
        {
            CodeExtractor.Compile("a").MatchTimeout.Should().Be(TimeSpan.FromSeconds(1));
        }
    }
}
