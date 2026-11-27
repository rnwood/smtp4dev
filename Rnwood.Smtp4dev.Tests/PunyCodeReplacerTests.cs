using AwesomeAssertions;
using Xunit;

namespace Rnwood.Smtp4dev.Tests
{
    public class PunyCodeReplacerTests
    {
        [Theory]
        [InlineData("user@xn--mnchen-3ya.de", "user@münchen.de")]
        [InlineData("user@xn--bcher-kva.example", "user@bücher.example")]
        [InlineData("user@XN--MNCHEN-3YA.DE", "user@münchen.DE")]
        [InlineData("user@xn--fiqs8s.cn", "user@中国.cn")]
        [InlineData("user@mail.xn--mnchen-3ya.de", "user@mail.münchen.de")]
        [InlineData("\"Max\" <max@xn--mnchen-3ya.de>, moritz@xn--bcher-kva.example", "\"Max\" <max@münchen.de>, moritz@bücher.example")]
        public void DecodesPunycodeLabels(string input, string expected)
        {
            PunyCodeReplacer.DecodePunycode(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user@foo-xn--bar.example")]
        [InlineData("user@xn--.example")]
        [InlineData("")]
        public void LeavesNonPunycodeUnchanged(string input)
        {
            PunyCodeReplacer.DecodePunycode(input).Should().Be(input);
        }

        [Fact]
        public void ReturnsNullForNull()
        {
            PunyCodeReplacer.DecodePunycode(null).Should().BeNull();
        }
    }
}
