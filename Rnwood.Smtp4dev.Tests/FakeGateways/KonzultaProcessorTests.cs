using System;
using System.Linq;
using System.Xml.Linq;
using AwesomeAssertions;
using Rnwood.Smtp4dev.FakeGateways;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.FakeGateways
{
    public class KonzultaProcessorTests
    {
        [Fact]
        public void Parse_SpecRequest_ReadsAllFieldsAndNestedDataItem()
        {
            var request = KonzultaProcessor.Parse(TestData.KonzultaSpecRequest);

            request.UserName.Should().Be("api-user");
            request.Items.Should().HaveCount(1);
            var item = request.Items[0];
            item.SendTime.Should().Be("20261117101530");
            item.MobileOriginate.Should().Be("SENDER");
            item.MobileTerminate.Should().Be("+420700000001");
            item.Text.Should().Contain("overovaci kod: 123456.");
            item.ContentType.Should().Be("Unicode");
            item.SmsId.Should().Be("987654");
        }

        [Fact]
        public void Parse_EmojiAndUnicodeContentType()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SmsServices><DataHeader><UserName>u</UserName></DataHeader><DataArray><DataTemplate>"
                + "<DataItem><MobileTerminate>+420700000001</MobileTerminate><Text>Ahoj \U0001F600 kód</Text><ContentType>Unicode</ContentType><SmsId>1</SmsId></DataItem>"
                + "</DataTemplate></DataArray></SmsServices>";

            var item = KonzultaProcessor.Parse(xml).Items.Single();

            item.Text.Should().Be("Ahoj \U0001F600 kód");
            item.ContentType.Should().Be("Unicode");
            SmsEncodingCalculator.Calculate(item.Text, item.ContentType).Encoding.Should().Be("UCS2");
        }

        [Fact]
        public void Parse_ThreeItemsInOneRequest()
        {
            string xml = TestData.KonzultaRequest(
                TestData.KonzultaItem("+420700000001", "a", "1"),
                TestData.KonzultaItem("+420700000002", "b", "2"),
                TestData.KonzultaItem("+420700000003", "c", "3"));

            var request = KonzultaProcessor.Parse(xml);

            request.Items.Select(i => i.SmsId).Should().Equal("1", "2", "3");
            request.Items.Select(i => i.MobileTerminate).Should().Equal("+420700000001", "+420700000002", "+420700000003");
        }

        [Fact]
        public void Parse_ItemWithoutSmsId_IsIncompleteOthersNot()
        {
            string xml = TestData.KonzultaRequest(
                "<DataItem><MobileTerminate>+420700000001</MobileTerminate><Text>a</Text></DataItem>",
                TestData.KonzultaItem("+420700000002", "b", "2"));

            var request = KonzultaProcessor.Parse(xml);

            request.Items.Should().HaveCount(2);
            KonzultaProcessor.IsIncomplete(request.Items[0]).Should().BeTrue();
            KonzultaProcessor.IsIncomplete(request.Items[1]).Should().BeFalse();
        }

        [Fact]
        public void Parse_XmlEntitiesInTextAreDecoded()
        {
            string xml = TestData.KonzultaRequest(TestData.KonzultaItem("+420700000001", "a &amp; b &lt;c&gt; &quot;d&quot;", "1"));

            KonzultaProcessor.Parse(xml).Items.Single().Text.Should().Be("a & b <c> \"d\"");
        }

        [Fact]
        public void Parse_InvalidXml_Throws()
        {
            Action act = () => KonzultaProcessor.Parse("<SmsServices><DataHeader>");
            act.Should().Throw<KonzultaRequestException>();
        }

        [Fact]
        public void Parse_WrongRoot_Throws()
        {
            Action act = () => KonzultaProcessor.Parse("<Other/>");
            act.Should().Throw<KonzultaRequestException>();
        }

        [Fact]
        public void Parse_DtdIsProhibited()
        {
            string xml = "<?xml version=\"1.0\"?><!DOCTYPE SmsServices [<!ENTITY x \"boom\">]><SmsServices>&x;</SmsServices>";
            Action act = () => KonzultaProcessor.Parse(xml);
            act.Should().Throw<KonzultaRequestException>();
        }

        [Fact]
        public void BuildResponse_ContainsDataItemWithSameSmsIdAndStatus()
        {
            string xml = KonzultaProcessor.BuildResponse(new[] { ("987654", 0, "fake-1"), ("55", 1, "fake-2") });

            xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            var items = XDocument.Parse(xml).Descendants("DataItem").ToList();
            items.Should().HaveCount(2);
            items[0].Element("SmsId").Value.Should().Be("987654");
            items[0].Element("Status").Value.Should().Be("0");
            items[0].Element("MsgId").Value.Should().Be("fake-1");
            items[1].Element("SmsId").Value.Should().Be("55");
            items[1].Element("Status").Value.Should().Be("1");
            XDocument.Parse(xml).Descendants("DataType").Single().Value.Should().Be("SMS");
        }

        [Theory]
        [InlineData("+420700000099", "+420700000099", true)]
        [InlineData("+420700000001", "+420700000099", false)]
        [InlineData("+420700000012", "+42070000*", true)]
        [InlineData("+421700000012", "+42070000*", false)]
        [InlineData("00420700000099", "+420700000099", true)]
        [InlineData("+420 700 000 099", "+420700000099", true)]
        [InlineData("+420700000099", "00420700000099", true)]
        public void IsRefusedNumber_MatchesGlobAfterNormalisation(string phone, string pattern, bool expected)
        {
            KonzultaProcessor.IsRefusedNumber(phone, new[] { pattern }).Should().Be(expected);
        }

        [Fact]
        public void IsRefusedNumber_EmptyOrInvalidPatterns_DoNotThrow()
        {
            KonzultaProcessor.IsRefusedNumber("+420700000001", null).Should().BeFalse();
            KonzultaProcessor.IsRefusedNumber("+420700000001", Array.Empty<string>()).Should().BeFalse();
            KonzultaProcessor.IsRefusedNumber("+420700000001", new[] { "", "[", "{a" }).Should().BeFalse();
        }
    }
}
