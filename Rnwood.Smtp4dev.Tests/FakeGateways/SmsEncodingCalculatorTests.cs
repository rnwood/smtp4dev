using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class SmsEncodingCalculatorTests
    {
        [Theory]
        [InlineData(0, "GSM7", 1)]
        [InlineData(1, "GSM7", 1)]
        [InlineData(160, "GSM7", 1)]
        [InlineData(161, "GSM7", 2)]
        [InlineData(306, "GSM7", 2)]
        [InlineData(307, "GSM7", 3)]
        public void Gsm7Text(int length, string encoding, int segments)
        {
            var result = SmsEncodingCalculator.Calculate(new string('a', length), null);
            result.Encoding.Should().Be(encoding);
            result.Segments.Should().Be(segments);
        }

        [Theory]
        [InlineData(70, 1)]
        [InlineData(71, 2)]
        [InlineData(134, 2)]
        [InlineData(135, 3)]
        public void Ucs2Text_NonGsmCharacter(int length, int segments)
        {
            var result = SmsEncodingCalculator.Calculate(new string('ě', length), null);
            result.Encoding.Should().Be("UCS2");
            result.Segments.Should().Be(segments);
        }

        [Theory]
        [InlineData(80, 1)]   // 80 x 2 septets = 160
        [InlineData(81, 2)]   // 162 septets
        public void ExtensionCharacterCountsTwice(int count, int segments)
        {
            var result = SmsEncodingCalculator.Calculate(new string('€', count), null);
            result.Encoding.Should().Be("GSM7");
            result.Segments.Should().Be(segments);
        }

        [Fact]
        public void ExtensionCharacterAddsToSeptetCount()
        {
            // 159 basic + 1 euro (2 septets) = 161 septets -> 2 segments
            SmsEncodingCalculator.Calculate(new string('a', 159) + "€", null).Segments.Should().Be(2);
            SmsEncodingCalculator.Calculate(new string('a', 158) + "€", null).Segments.Should().Be(1);
        }

        [Fact]
        public void GsmEscapePairIsNotSplitAcrossSegments()
        {
            // 152 + euro(2) + 152 = 306 septets; naive ceil(306/153) = 2, but the euro does not fit into the first segment
            // (152 + 2 > 153) and the second one (2 + 152 > 153) -> 3 segments.
            SmsEncodingCalculator.Calculate(new string('a', 152) + "€" + new string('a', 152), null).Segments.Should().Be(3);
            // 152 + euro + 151 = 305 septets still fits into 2 segments (152 | 2 + 151)
            SmsEncodingCalculator.Calculate(new string('a', 152) + "€" + new string('a', 151), null).Segments.Should().Be(2);
        }

        [Fact]
        public void SurrogatePairIsNotSplitAcrossSegments()
        {
            // 66 + emoji(2) + 66 = 134 UTF-16 units; naive ceil(134/67) = 2, but the emoji does not fit into the first segment
            // (66 + 2 > 67) and the second one (2 + 66 > 67) -> 3 segments.
            string text = new string('ě', 66) + "\U0001F600" + new string('ě', 66);
            var result = SmsEncodingCalculator.Calculate(text, null);
            result.Encoding.Should().Be("UCS2");
            result.Segments.Should().Be(3);
            SmsEncodingCalculator.Calculate(new string('ě', 66) + "\U0001F600" + new string('ě', 65), null).Segments.Should().Be(2);
        }

        [Fact]
        public void UnicodeContentType_ForcesUcs2()
        {
            var result = SmsEncodingCalculator.Calculate("plain ascii", "Unicode");
            result.Encoding.Should().Be("UCS2");
            result.Segments.Should().Be(1);
        }

        [Fact]
        public void Emoji_IsUcs2AndCountsTwoUtf16Units()
        {
            var result = SmsEncodingCalculator.Calculate(new string('a', 69) + "\U0001F600", null);
            result.Encoding.Should().Be("UCS2");
            result.Segments.Should().Be(2); // 71 UTF-16 units
        }
    }
}
